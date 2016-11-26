using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace OKEGui
{
    public enum EACProgressType
    {
        Analyze,
        Process,
        Completed,
    }

    public class EACDemuxer
    {
        public enum ProcessState
        {
            FetchStream,
            ExtractStream,
        }

        public enum TrackType
        {
            Unknown,
            H264_AVC,
            RAW_PCM,
            DTSMA,
            TRUEHD_AC3,
            PGS,
            Chapter,
        }

        public struct TrackInfo
        {
            public TrackType Type { get; set; }
            public int Index { get; set; }
            public string Information { get; set; }
            public string RawOutput { get; set; }
            public string SourceFile { get; set; }

            public string OutFileName
            {
                get {
                    var directory = Path.GetDirectoryName(SourceFile);
                    var baseName = Path.GetFileNameWithoutExtension(SourceFile);

                    return $"{Path.Combine(directory, baseName)}_{Index}{FileExtension}";
                }
            }

            public string FileExtension
            {
                get {
                    TrackType type = Type;
                    return "." + s_eacOutputs.Find(val => val.Type == type).FileExtension;
                }
            }
        }

        private struct EacOutputTrackType
        {
            public TrackType Type { get; set; }
            public string RawOutput { get; set; }
            public string FileExtension { get; set; }
            public bool Extract { get; set; }

            public EacOutputTrackType(TrackType type, string rawOutput, string extension, bool extract)
            {
                Type = type;
                RawOutput = rawOutput;
                FileExtension = extension;
                Extract = extract;
            }
        }

        private string _eacPath;
        private List<TrackInfo> tracks = new List<TrackInfo>();
        private Action<double, EACProgressType> _progressCallback;
        private Process proc = new Process();
        private ManualResetEvent mre = new ManualResetEvent(false);
        private ProcessState state;
        private string sourceFile;

        private static List<EacOutputTrackType> s_eacOutputs = new List<EacOutputTrackType> {
            new EacOutputTrackType(TrackType.RAW_PCM,    "RAW/PCM",          "flac",    true),
            new EacOutputTrackType(TrackType.DTSMA,      "DTS Master Audio", "flac",    true),

            new EacOutputTrackType(TrackType.TRUEHD_AC3, "TrueHD/AC3",       "thd",     true),

            new EacOutputTrackType(TrackType.H264_AVC,   "h264/AVC",         "h264",    false),
            new EacOutputTrackType(TrackType.PGS,        "Subtitle (PGS)",   "sup",     true),
            new EacOutputTrackType(TrackType.Chapter,    "Chapters",         "txt",     true),
        };

        public EACDemuxer(string eacPath, string fileName)
        {
            _eacPath = eacPath;
            sourceFile = fileName;
        }

        private void StartEac(string arguments, bool asyncRead)
        {
            var startInfo = new ProcessStartInfo {
                FileName = _eacPath,
                Arguments = arguments + " -progressnumbers",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            proc.StartInfo = startInfo;
            try {
                proc.Start();
                if (asyncRead) {
                    new Thread(new ThreadStart(readStdErr)).Start();
                    new Thread(new ThreadStart(readStdOut)).Start();
                    proc.WaitForExit();
                } else {
                    proc.WaitForExit();
                    readStream(proc.StandardOutput);
                }
            } catch (Exception e) { throw e; }
        }

        private void readStream(StreamReader sr)
        {
            string line;
            if (proc != null) {
                try {
                    while ((line = sr.ReadLine()) != null) {
                        if (state == ProcessState.FetchStream) {
                            DetectFileTracks(line);
                            _progressCallback(0, EACProgressType.Analyze);
                        } else if (state == ProcessState.ExtractStream) {
                            Regex rAnalyze = new Regex("analyze: ([0-9]+)%");
                            Regex rProgress = new Regex("process: ([0-9]+)%");

                            double p = 0;
                            if (rAnalyze.IsMatch(line)) {
                                double.TryParse(rAnalyze.Split(line)[1], out p);
                                if (p > 1) {
                                    _progressCallback(p, EACProgressType.Analyze);
                                }
                            } else if (rProgress.IsMatch(line)) {
                                double.TryParse(rProgress.Split(line)[1], out p);
                                if (p > 1) {
                                    _progressCallback(p, EACProgressType.Process);
                                }
                            }

                            if (line.ToLower().Contains("done.")) {
                                _progressCallback(100, EACProgressType.Completed);
                            }
                        }
                    }
                } catch (Exception e) {
                    throw e;
                }
            }
        }

        private void readStdOut()
        {
            StreamReader sr = null;
            try {
                sr = proc.StandardOutput;
            } catch (Exception e) {
                // log.LogValue("Exception getting IO reader for stdout", e, ImageType.Error);
                return;
            }
            readStream(sr);
        }

        private void readStdErr()
        {
            StreamReader sr = null;
            try {
                sr = proc.StandardError;
            } catch (Exception e) {
                // log.LogValue("Exception getting IO reader for stderr", e, ImageType.Error);
                return;
            }
            readStream(sr);
        }

        private TrackType EacOutputToTrackType(string str)
        {
            str = str.Trim();
            EacOutputTrackType outputType = s_eacOutputs.Find(val => val.RawOutput == str);
            return outputType.Type;
        }

        private void DetectFileTracks(string line)
        {
            line.Trim();
            if (string.IsNullOrEmpty(line)) return;

            if (Regex.IsMatch(line, @"^\d*?: .*$")) {
                var match = Regex.Match(line, @"^(\d*?): (.*?), (.*?)$");
                if (match.Groups.Count < 4) return;

                var trackInfo = new TrackInfo {
                    Index = Convert.ToInt32(match.Groups[1].Value),
                    Type = EacOutputToTrackType(match.Groups[2].Value),
                    Information = match.Groups[3].Value.Trim(),
                    RawOutput = line,
                    SourceFile = sourceFile,
                };

                if (TrackType.Unknown == trackInfo.Type) {
                    throw new ArgumentException($"不明类型: {trackInfo.RawOutput}");
                }
                tracks.Add(trackInfo);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="completedCallback">抽取的轨道，不包含重复轨道；重复轨道文件名带有bak</param>
        public List<TrackInfo> Extract(Action<double, EACProgressType> progressCallback)
        {
            if (!new FileInfo(sourceFile).Exists) {
                return null;
            }
            _progressCallback = progressCallback;

            state = ProcessState.FetchStream;
            StartEac($"\"{sourceFile}\"", false);

            var args = new List<string>();
            var extractResult = new List<TrackInfo>();

            foreach (var track in tracks) {
                if (!s_eacOutputs.Find(val => val.Type == track.Type).Extract) {
                    continue;
                };

                args.Add($"{track.Index}:\"{track.OutFileName}\"");
                extractResult.Add(track);
            }

            state = ProcessState.ExtractStream;
            StartEac($"\"{sourceFile}\" {string.Join(" ", args)}", true);

            Dictionary<int, long> trackSize = new Dictionary<int, long>();

            foreach (var track in extractResult) {
                FileInfo finfo = new FileInfo(track.OutFileName);
                if (!finfo.Exists && finfo.Length > 0) {
                    throw new Exception("文件输出失败: " + track.OutFileName);
                }

                trackSize.Add(track.Index, finfo.Length);
            }

            List<int> removeList = new List<int>();
            foreach (var item in trackSize) {
                if (removeList.Contains(item.Key)) {
                    continue;
                }

                foreach (var citem in trackSize) {
                    if (citem.Key == item.Key ||
                        Math.Abs(item.Value - citem.Value) > 1024) {
                        continue;
                    }

                    var ctrack = extractResult.Find(t => { return t.Index == citem.Key; });
                    if (ctrack.Index == citem.Key) {
                        removeList.Add(ctrack.Index);
                        File.Move(ctrack.OutFileName, Path.ChangeExtension(ctrack.OutFileName, ".bak") + ctrack.FileExtension);
                        extractResult.Remove(ctrack);
                    }
                }
            }

            return extractResult;
        }
    }
}
