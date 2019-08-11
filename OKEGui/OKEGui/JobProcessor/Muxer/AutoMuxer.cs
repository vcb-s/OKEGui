using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace OKEGui
{
    public class AutoMuxer : IMediaMuxer
    {
        private enum OutputType
        {
            Mkv,
            Mp4
        }

        private struct Episode
        {
            public string VideoFile { get; set; }
            public string VideoFps { get; set; }

            public List<string> AudioFiles { get; set; }
            public List<string> AudioLanguages { get; set; }

            public string ChapterFile { get; set; }

            public List<string> SubtitleFiles { get; set; }
            public List<string> SubtitleLanguages { get; set; }

            public OutputType OutputFileType { get; set; }
            public string OutputFile { get; set; }

            public long TotalFileSize { get; set; }
        }

        private static List<string> s_AudioFileExtensions = new List<string> {
            ".flac", ".wav", ".ac3", ".dts", ".aac", ".m4a"
        };

        private static List<string> s_VideoFileExtensions = new List<string> {
            ".hevc", ".h265", ".avc", ".h264"
        };

        private string _mkvMergePath;
        private string _mp4MuxerPath;
        private Episode _episode;
        private Process proc = new Process();
        private ManualResetEvent mre = new ManualResetEvent(false);

        public delegate void MuxingProgressChangedEventHandler(double progress);

        public event MuxingProgressChangedEventHandler ProgressChanged;

        public AutoMuxer(string mkvMergePath, string mp4MuxerPath)
        {
            _mkvMergePath = mkvMergePath;
            _mp4MuxerPath = mp4MuxerPath;
        }

        private Episode GenerateEpisode(
            List<string> inputFileNames,
            string outputFileName,
            string videoFps/* = "24000/1001"*/,
            List<string> audioLanguages/* = "jpn"*/,
            List<string> subtitleLanguages/* = "jpn"*/
        )
        {
            var episode = new Episode {
                AudioFiles = new List<string>(),
                SubtitleFiles = new List<string>(),
                VideoFps = videoFps,
                AudioLanguages = audioLanguages,
                SubtitleLanguages = subtitleLanguages,
                TotalFileSize = 0
            };

            foreach (string file in inputFileNames) {
                FileInfo fileInfo = new FileInfo(file);
                if (!fileInfo.Exists) {
                    continue;
                }

                var extension = Path.GetExtension(file).ToLower();

                if (s_AudioFileExtensions.Contains(extension)) {
                    episode.AudioFiles.Add(file);
                    episode.TotalFileSize += fileInfo.Length;
                    continue;
                }
                if (s_VideoFileExtensions.Contains(extension)) {
                    episode.VideoFile = file;
                    episode.TotalFileSize += fileInfo.Length;
                    continue;
                }
                switch (extension) {
                    case ".txt":
                        episode.ChapterFile = file;
                        break;

                    case ".sup":
                        episode.SubtitleFiles.Add(file);
                        break;
                }
            }

            episode.OutputFile = outputFileName;
            switch (Path.GetExtension(outputFileName).ToLower()) {
                case ".mkv":
                    episode.OutputFileType = OutputType.Mkv;
                    break;

                case ".mp4":
                    episode.OutputFileType = OutputType.Mp4;
                    break;

                default:
                    throw new ArgumentException("输出文件扩展名不正确");
            }
            return episode;
        }

        private string GenerateMkvMergeParameter(Episode episode)
        {
            string trackTemplate = "--language 0:{0} \"(\" \"{1}\" \")\"";

            var parameters = new List<string>();
            var trackOrder = new List<string>();

            int fileID = 0;

            // parameters.Add("--ui-language zh_CN");
            parameters.Add($"--output \"{episode.OutputFile}\"");

            parameters.Add(string.Format(trackTemplate, "und", episode.VideoFile));
            trackOrder.Add($"{fileID++}:0");

            for (int i = 0; i < episode.AudioFiles.Count; i++) {
                string audioFile = episode.AudioFiles[i];
                parameters.Add(string.Format(trackTemplate, episode.AudioLanguages[i], audioFile));
                trackOrder.Add($"{fileID++}:0");
            }

            if (episode.SubtitleFiles != null) {
                for (int i = 0; i < episode.SubtitleFiles.Count; i++) {
                    string subtitleFile = episode.SubtitleFiles[i];
                    parameters.Add(string.Format(trackTemplate, episode.SubtitleLanguages[i], subtitleFile));
                    trackOrder.Add($"{fileID++}:0");
                }
            }

            if (episode.ChapterFile != null) parameters.Add($"--chapters \"{episode.ChapterFile}\"");

            parameters.Add(string.Format("--track-order {0}", string.Join(",", trackOrder)));

            return string.Join(" ", parameters);
        }

        private string GenerateMp4MergeParameter(Episode episode)
        {
            var parameters = new List<string>();
            parameters.Add("--file-format mp4");

            if (episode.ChapterFile != null) parameters.Add($"--chapter \"{episode.ChapterFile}\"");

            parameters.Add($"-i \"{episode.VideoFile}\"?fps={episode.VideoFps}");

            for (int i = 0; i < episode.AudioFiles.Count; i++) {
                string audioFile = episode.AudioFiles[i];
                FileInfo ainfo = new FileInfo(audioFile);
                if (ainfo.Extension.ToLower() == ".aac" || ainfo.Extension.ToLower() == ".m4a" || ainfo.Extension.ToLower() == ".ac3" || ainfo.Extension.ToLower() == ".dts") {
                    parameters.Add($"-i \"{audioFile}\"?language={episode.AudioLanguages[i]}");
                }
            }

            parameters.Add($"-o \"{episode.OutputFile}\"");

            return string.Join(" ", parameters);
        }

        private void StartProcess(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            proc.StartInfo = startInfo;

            try {
                proc.Start();
                new Thread(new ThreadStart(readStdErr)).Start();
                new Thread(new ThreadStart(readStdOut)).Start();
                proc.WaitForExit();
                mre.WaitOne();
            } catch (Exception e) { throw e; }
        }

        private void readStream(StreamReader sr)
        {
            string line;
            if (null == proc) return;

            try {
                while ((line = sr.ReadLine()) != null) {
                    Debugger.Log(0, "ReadStream", line + "\n");

                    Match progressMatch;
                    double progress = -1;

                    switch (_episode.OutputFileType) {
                        case OutputType.Mkv:
                            if (line.Contains("Progress: ")) {
                                progressMatch = Regex.Match(line, @"Progress: (\d*?)%", RegexOptions.Compiled);
                                if (progressMatch.Groups.Count < 2) return;
                                progress = double.Parse(progressMatch.Groups[1].Value);
                            } else if (line.Contains("Muxing took") || line.Contains("Multiplexing took")) { //different versions of mkvmerge may return different wordings. Muxing took is the old way.
                                mre.Set();
                            }
                            break;

                        case OutputType.Mp4:
                            if (line.Contains("Importing: ")) {
                                progressMatch = Regex.Match(line, @"Importing: (\d*?) bytes", RegexOptions.Compiled);
                                if (progressMatch.Groups.Count < 2) return;
                                progress = Convert.ToDouble(double.Parse(progressMatch.Groups[1].Value) / _episode.TotalFileSize * 100d);
                            } else if (line.Contains("Muxing completed")) {
                                mre.Set();
                            }
                            break;
                    }
                    if (progress > -1 && ProgressChanged != null) {
                        ProgressChanged(progress);
                    }
                }
            } catch (Exception) {
                throw;
            }
        }

        private void readStdOut()
        {
            StreamReader sr = null;
            try {
                sr = proc.StandardOutput;
            } catch (Exception e) {
                Debugger.Log(0, "", "Exception getting IO reader for stdout" + e.ToString());
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
                Debugger.Log(0, "", "Exception getting IO reader for stderr" + e.ToString());
                return;
            }
            readStream(sr);
        }

        public void StartMerge(
            List<string> inputFileNames,
            string outputFileName,
            string videoFps,
            List<string> audioLanguages,
            List<string> subtitleLanguages
        )
        {
            _episode = GenerateEpisode(inputFileNames, outputFileName, videoFps, audioLanguages, subtitleLanguages);
            string mainProgram = string.Empty;
            string args = string.Empty;
            switch (_episode.OutputFileType) {
                case OutputType.Mkv:
                    mainProgram = _mkvMergePath;
                    args = GenerateMkvMergeParameter(_episode);
                    break;

                case OutputType.Mp4:
                    mainProgram = _mp4MuxerPath;
                    args = GenerateMp4MergeParameter(_episode);
                    break;
            }
            StartProcess(mainProgram, args);
        }

        public IFile StartMuxing(string path, MediaFile mediaFile)
        {
            List<string> input = new List<string>();
            string videoFps = "";
            List<string> audioLanguages = new List<string>();
            List<string> subtitleLanguages = new List<string>();

            foreach (var track in mediaFile.Tracks) {
                if (track.IsDisable) {
                    continue;
                }
                switch (track.TrackType)
                {
                    case TrackType.Audio:
                        AudioTrack audioTrack = track as AudioTrack;
                        audioLanguages.Add(audioTrack.AudioInfo.Language);
                        break;
                    case TrackType.Video:
                        VideoTrack videoTrack = track as VideoTrack;
                        videoFps = $"{videoTrack.VideoInfo.FpsNum}/{videoTrack.VideoInfo.FpsDen}";
                        break;
                    case TrackType.Subtitle:
                        SubtitleTrack subtitleTrack = track as SubtitleTrack;
                        subtitleLanguages.Add(subtitleTrack.Language);
                        break;
                }
                    
                input.Add(track.File.GetFullPath());
            }

            this.StartMerge(input, path, videoFps, audioLanguages, subtitleLanguages);

            IFile outFile = new OKEFile(path);
            outFile.AddCRC32();

            return outFile.Exists() ? outFile : null;
        }

        public bool IsCompatible(MediaFile mediaFile)
        {
            // TODO:
            // 更加彻底的检查
            return mediaFile.VideoTrack == null;
        }
    }
}
