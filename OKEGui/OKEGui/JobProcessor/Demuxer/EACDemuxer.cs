using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Model;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public enum EACProgressType
    {
        Analyze,
        Process,
        Completed,
    }

    public partial class EACDemuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("EACDemuxer");

        public enum ProcessState
        {
            FetchStream,
            ExtractStream,
        }

        private struct EacOutputTrackType
        {
            public TrackCodec Codec { get; set; }
            public string RawOutput { get; set; }
            public string FileExtension { get; set; }
            public bool Extract { get; set; }

            public TrackType Type { get; set; }

            public EacOutputTrackType(TrackCodec codec, string rawOutput, string extension, bool extract, TrackType type)
            {
                Codec = codec;
                RawOutput = rawOutput;
                FileExtension = extension;
                Extract = extract;
                Type = type;
            }
        }

        private string _eacPath;
        private List<TrackInfo> tracks = new List<TrackInfo>();
        private Action<double, EACProgressType> _progressCallback;
        private Process proc = new Process();
        private ManualResetEvent mre = new ManualResetEvent(false);
        private ProcessState state;
        private string sourceFile;
        private List<AudioInfo> JobAudio;
        private List<Info> JobSub;
        private int length;
        private string WorkingPathPrefix;
        private bool SkipAllAudioTracks;
        private bool SkipAllSubtitleTracks;

        private static List<EacOutputTrackType> s_eacOutputs = new List<EacOutputTrackType> {
            new EacOutputTrackType(TrackCodec.RAW_PCM,    "RAW/PCM",            "flac",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.DTSMA,      "DTS Master Audio",   "flac",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.TRUEHD_AC3, "TrueHD/AC3",         "flac",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.TRUEHD_AC3, "TrueHD",             "flac",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.AC3,        "AC3",                "ac3",     true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.FLAC,       "FLAC",               "flac",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.AAC,        "AAC",                "aac",     true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.DTS,        "DTS",                "dts",     true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.EAC3,       "EAC3",               "eac3",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.EAC3,       "E-AC3",              "eac3",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.OPUS,       "OPUS",               "opus",    true,  TrackType.Audio),
            new EacOutputTrackType(TrackCodec.MPEG2,      "MPEG2",              "m2v",     false, TrackType.Video),
            new EacOutputTrackType(TrackCodec.H264_AVC,   "h264/AVC",           "264",     false, TrackType.Video),
            new EacOutputTrackType(TrackCodec.H265_HEVC,  "h265/HEVC",          "265",     false, TrackType.Video),
            new EacOutputTrackType(TrackCodec.AV1,        "AV1",                "av1",     false, TrackType.Video),
            new EacOutputTrackType(TrackCodec.PGS,        "Subtitle (PGS)",     "sup",     true,  TrackType.Subtitle),
            new EacOutputTrackType(TrackCodec.ASS,        "Subtitle (ASS)",     "ass",     true,  TrackType.Subtitle),
            new EacOutputTrackType(TrackCodec.SRT,        "Subtitle (SRT)",     "srt",     true,  TrackType.Subtitle),
            new EacOutputTrackType(TrackCodec.Chapter,    "Chapters",           "txt",     false, TrackType.Chapter),
            new EacOutputTrackType(TrackCodec.VobSub,     "Subtitle (VobSub)",  "sub",     false, TrackType.Subtitle) //vobsub is not supported by eac3to
        };

        public EACDemuxer(string eacPath, string fileName, TaskProfile jobProfile)
        {
            _eacPath = eacPath;
            sourceFile = fileName;
            JobAudio = new List<AudioInfo>();
            if (jobProfile.AudioTracks != null && !jobProfile.SkipAllAudioTracks)
            {
                JobAudio.AddRange(jobProfile.AudioTracks);
            }
            JobSub = new List<Info>();
            if (jobProfile.SubtitleTracks != null && !jobProfile.SkipAllSubtitleTracks)
            {
                JobSub.AddRange(jobProfile.SubtitleTracks);
            }
            WorkingPathPrefix = jobProfile.WorkingPathPrefix;
            SkipAllAudioTracks = jobProfile.SkipAllAudioTracks;
            SkipAllSubtitleTracks = jobProfile.SkipAllSubtitleTracks;

            Logger.Debug($"SkipAllAudioTracks: {jobProfile.SkipAllAudioTracks}, SkipAllSubtitleTracks: {jobProfile.SkipAllSubtitleTracks}");
        }

        private void StartEac(string arguments, string logPath, bool asyncRead)
        {
            // Since eac3to does not support log paths containing spaces, we first calculate a short hash 
            // from the intended log path to serve as the log filename. We then launch eac3to in the 
            // working directory to generate the log, and finally rename the log file back to its full name.
            var workingPathDir = Path.GetDirectoryName(WorkingPathPrefix);
            var logPathFull = Path.Combine(workingPathDir, logPath);
            byte[] buffer = Encoding.UTF8.GetBytes(logPathFull);
            var crc = CRC32.Compute(buffer);
            var logHashPath = crc.ToString("X8") + ".eac3to.log";

            Logger.Trace($"workingPathDir: {workingPathDir}");
            Logger.Trace($"eac3to logPathFull: {logPathFull}");
            Logger.Trace($"eac3to logHashPath: {logHashPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = _eacPath,
                Arguments = arguments + $" -log=\"{logHashPath}\"" + " -progressnumbers",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                WorkingDirectory = workingPathDir,
            };
            Logger.Info(startInfo.FileName + " " + startInfo.Arguments);

            proc.StartInfo = startInfo;
            try
            {
                proc.Start();
                if (asyncRead)
                {
                    new Thread(new ThreadStart(readStdErr)).Start();
                    new Thread(new ThreadStart(readStdOut)).Start();
                    proc.WaitForExit();
                }
                else
                {
                    readStream(proc.StandardOutput);
                    proc.WaitForExit();
                }

                FileInfo logFile = new FileInfo(Path.Combine(workingPathDir, logHashPath));
                if (logFile.Exists)
                {
                    FileInfo logFileFinal = new FileInfo(logPathFull);
                    if (logFileFinal.Exists)
                    {
                        logFileFinal.Delete();
                    }
                    logFile.MoveTo(logPathFull);
                }

                if (proc.ExitCode != 0)
                {
                    OKETaskException ex = new OKETaskException(Constants.eac3toErrorSmr);
                    ex.Data["EXIT_CODE"] = proc.ExitCode;
                    throw ex;
                }
            }
            catch (Exception e) { throw e; }
        }

        private void readStream(StreamReader sr)
        {
            string line;
            if (proc != null)
            {
                try
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (state == ProcessState.FetchStream)
                        {
                            DetectFileTracks(line);
                            _progressCallback(0, EACProgressType.Analyze);
                        }
                        else if (state == ProcessState.ExtractStream)
                        {
                            Logger.Trace(line);
                            Regex rAnalyze = new Regex("analyze: ([0-9]+)%");
                            Regex rProgress = new Regex("process: ([0-9]+)%");

                            double p = 0;
                            if (rAnalyze.IsMatch(line))
                            {
                                double.TryParse(rAnalyze.Split(line)[1], out p);
                                if (p > 1)
                                {
                                    _progressCallback(p, EACProgressType.Analyze);
                                }
                            }
                            else if (rProgress.IsMatch(line))
                            {
                                double.TryParse(rProgress.Split(line)[1], out p);
                                if (p > 1)
                                {
                                    _progressCallback(p, EACProgressType.Process);
                                }
                            }

                            if (line.ToLower().Contains("done."))
                            {
                                _progressCallback(100, EACProgressType.Completed);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show(e.ToString());
                    throw e;
                }
            }
        }

        private void readStdOut()
        {
            StreamReader sr = null;
            try
            {
                sr = proc.StandardOutput;
            }
            catch (Exception e)
            {
                Debugger.Log(0, "", "Exception getting IO reader for stdout" + e.ToString());
                return;
            }
            readStream(sr);
        }

        private void readStdErr()
        {
            StreamReader sr = null;
            try
            {
                sr = proc.StandardError;
            }
            catch (Exception e)
            {
                Debugger.Log(0, "", "Exception getting IO reader for stderr" + e.ToString());
                return;
            }
            readStream(sr);
        }

        private TrackCodec EacOutputToTrackCodec(string str)
        {
            str = str.Trim();
            EacOutputTrackType outputType = s_eacOutputs.Find(val => str.StartsWith(val.RawOutput));
            return outputType.Codec;
        }

        private TrackType EacOutputToTrackType(string str)
        {
            str = str.Trim();
            EacOutputTrackType outputType = s_eacOutputs.Find(val => str.StartsWith(val.RawOutput));
            return outputType.Type;
        }

        private void DetectFileTracks(string line)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) return;
            Logger.Trace(line);
            if (Regex.IsMatch(line, @"\d*:\d*:\d*"))
            {
                string[] match = Regex.Split(line, @"(\d*):(\d*):(\d*)");
                int hour = int.Parse(match[1]);
                int minute = int.Parse(match[2]);
                int second = int.Parse(match[3]);
                length = hour * 3600 + minute * 60 + second;
            }

            if (Regex.IsMatch(line, @"^\d*?: .*$"))
            {
                //原盘没有信息的PGS字幕
                if (line.Contains("PGS") && !line.Contains(","))
                    line += ", Japanese";

                var match = Regex.Match(line, @"^(\d*?): (.*?), (.*?)$");

                if (match.Groups.Count < 4)
                {
                    return;
                }
                Logger.Debug(line);
                var trackInfo = new TrackInfo
                {
                    Index = Convert.ToInt32(match.Groups[1].Value),
                    Codec = EacOutputToTrackCodec(match.Groups[2].Value),
                    Information = match.Groups[3].Value.Trim(),
                    Length = length,
                    RawOutput = line,
                    SourceFile = sourceFile,
                    WorkingPathPrefix = WorkingPathPrefix,
                    Type = EacOutputToTrackType(match.Groups[2].Value),
                    DupOrEmpty = false
                };

                if (TrackCodec.Unknown == trackInfo.Codec)
                {
                    throw new ArgumentException($"不明类型: {trackInfo.RawOutput}");
                }
                tracks.Add(trackInfo);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="completedCallback">抽取的轨道，不包含重复轨道；重复轨道文件名带有.bak</param>
        public MediaFile Extract(Action<double, EACProgressType> progressCallback)
        {
            if (!new FileInfo(sourceFile).Exists)
            {
                return null;
            }

            var baseName = Path.GetFileNameWithoutExtension(sourceFile);
            string logPath = $"{baseName}.eac3to.log";

            _progressCallback = progressCallback;

            state = ProcessState.FetchStream;
            StartEac($"\"{sourceFile}\"", logPath, false);

            if (SkipAllAudioTracks)
            {
                tracks.RemoveAll(track => track.Type == TrackType.Audio);
                Logger.Debug("Skip all audio tracks");
            }
            if (SkipAllSubtitleTracks)
            {
                tracks.RemoveAll(track => track.Type == TrackType.Subtitle);
                Logger.Debug("Skip all subtitle tracks");
            }

            var args = new List<string>();
            var extractResult = new List<TrackInfo>();
            List<TrackInfo> srcAudio = new List<TrackInfo>();
            List<TrackInfo> srcSub = new List<TrackInfo>();

            foreach (TrackInfo track in tracks)
            {
                switch (track.Type)
                {
                    case TrackType.Audio:
                        srcAudio.Add(track);
                        break;
                    case TrackType.Subtitle:
                        srcSub.Add(track);
                        break;
                    default:
                        break;
                }
            }
            List<AudioInfo> requiredAudios = JobAudio.FindAll(val => !val.Optional);
            if (srcAudio.Count != JobAudio.Count && srcAudio.Count != requiredAudios.Count)
            {
                OKETaskException ex = new OKETaskException(Constants.audioNumMismatchSmr);
                ex.progress = 0.0;
                ex.Data["SRC_TRACK"] = srcAudio.Count;
                ex.Data["DST_REQ_TRACK"] = requiredAudios.Count;
                ex.Data["DST_OPT_TRACK"] = JobAudio.Count - requiredAudios.Count;
                throw ex;
            }
            List<Info> requiredSubs = JobSub.FindAll(val => !val.Optional);
            if (srcSub.Count != JobSub.Count && srcSub.Count != requiredSubs.Count)
            {
                OKETaskException ex = new OKETaskException(Constants.subNumMismatchSmr);
                ex.progress = 0.0;
                ex.Data["SRC_TRACK"] = srcSub.Count;
                ex.Data["DST_REQ_TRACK"] = requiredSubs.Count;
                ex.Data["DST_OPT_TRACK"] = JobSub.Count - requiredSubs.Count;
                throw ex;
            }
            if (srcAudio.Count != JobAudio.Count)
                JobAudio.RemoveAll(info => info.Optional);
            if (srcSub.Count != JobSub.Count)
                JobSub.RemoveAll(info => info.Optional);

            int audioId = 0, subId = 0;
            foreach (TrackInfo track in tracks)
            {
                EacOutputTrackType trackType = s_eacOutputs.Find(val => val.Codec == track.Codec);
                if (!trackType.Extract)
                {
                    continue;
                };
                if (track.Type == TrackType.Audio)
                {
                    AudioInfo jobAudioInfo = JobAudio[audioId++];
                    if (jobAudioInfo.MuxOption == MuxOption.Skip)
                    {
                        continue;
                    }
                    if (jobAudioInfo.Lossy)
                        if (track.Codec != TrackCodec.EAC3)
                            track.Codec = TrackCodec.FLAC;
                }
                if (track.Type == TrackType.Subtitle)
                {
                    Info jobSubInfo = JobSub[subId++];
                    if (jobSubInfo.MuxOption == MuxOption.Skip)
                    {
                        continue;
                    }
                }

                args.Add($"{track.Index}:\"{track.OutFileName}\"");
                extractResult.Add(track);
            }
            JobAudio.RemoveAll(info => info.MuxOption == MuxOption.Skip);
            JobSub.RemoveAll(info => info.MuxOption == MuxOption.Skip);

            state = ProcessState.ExtractStream;
            StartEac($"\"{sourceFile}\" {string.Join(" ", args)}", logPath, true);

            foreach (TrackInfo track in extractResult)
            {
                FileInfo finfo = new FileInfo(track.OutFileName);
                if (!finfo.Exists || finfo.Length == 0)
                {
                    throw new Exception("文件输出失败: " + track.OutFileName);
                }
                else
                {
                    track.FileSize = finfo.Length;
                }
                if (track.Type == TrackType.Audio)
                {
                    FFmpegVolumeChecker checker = new FFmpegVolumeChecker(track.OutFileName);
                    checker.start();
                    checker.waitForFinish();
                    track.MaxVolume = checker.MaxVolume;
                    track.MeanVolume = checker.MeanVolume;
                }
            }

            List<int> removeList = new List<int>();
            for (int i = 0; i < extractResult.Count; i++)
            {
                TrackInfo track = extractResult[i];
                if (removeList.Contains(track.Index))
                {
                    continue;
                }
                if (track.IsEmpty())
                {
                    Logger.Warn(track.OutFileName + "被检测为空轨道。");
                    removeList.Add(track.Index);
                    track.MarkSkipping();
                    continue;
                }

                for (int j = i + 1; j < extractResult.Count; j++)
                {
                    TrackInfo other = extractResult[j];
                    if (track.IsDuplicate(other))
                    {
                        Logger.Warn(track.OutFileName + "被检测为与" + other.OutFileName + "重复。");
                        removeList.Add(other.Index);
                        other.MarkSkipping();
                    }
                }
            }

            MediaFile mf = new MediaFile();
            audioId = 0;
            subId = 0;
            foreach (var item in extractResult)
            {
                OKEFile file = new OKEFile(item.OutFileName);
                switch (item.Type)
                {
                    case TrackType.Audio:
                        AudioInfo audioInfo = JobAudio[audioId++];
                        audioInfo.DupOrEmpty = item.DupOrEmpty;
                        audioInfo.Length = item.Length;
                        mf.AddTrack(new AudioTrack(file, audioInfo));
                        break;
                    case TrackType.Subtitle:
                        Info subInfo = JobSub[subId++];
                        subInfo.DupOrEmpty = item.DupOrEmpty;
                        mf.AddTrack(new SubtitleTrack(file, subInfo));
                        break;
                    case TrackType.Chapter:
                        FileInfo txtChapter = new FileInfo(Path.ChangeExtension(sourceFile, ".txt"));
                        if (txtChapter.Exists)
                        {
                            Logger.Info("检测到单独准备的章节，不予添加");
                        }
                        else
                        {
                            file.Rename(txtChapter.FullName);
                        }
                        break;
                    case TrackType.Video:
                        mf.AddTrack(new VideoTrack(file, new VideoInfo()));
                        break;
                    default:
                        System.Windows.MessageBox.Show(item.OutFileName, "不认识的轨道呢");
                        break;
                }
            }

            Logger.Debug($"extract audio number: {audioId}, extract sub number: {subId}");

            return mf;
        }
    }
}
