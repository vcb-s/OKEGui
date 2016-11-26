using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace OKEGui
{
    class AutoMuxer /*: CommandlineJobProcessor<MuxJob>*/
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
            public string AudioLanguage { get; set; }

            public string ChapterFile { get; set; }

            public List<string> SubtitleFiles { get; set; }
            public string SubtitleLanguage { get; set; }

            public OutputType OutputFileType { get; set; }
            public string OutputFile { get; set; }

            public long TotalFileSize { get; set; }
        }

        private static List<string> s_AudioFileExtensions = new List<string> {
            ".flac", ".wav", ".ac3", ".thd", ".aac", ".m4a"
        };

        private static List<string> s_VideoFileExtensions = new List<string> {
            ".hevc", ".h265", ".avc", ".h264"
        };

        private string _mkvMergePath;
        private string _mp4MuxerPath;
        private Episode _episode;
        private Process proc = new Process();
        private ManualResetEvent mre = new ManualResetEvent(false);

        public AutoMuxer(string mkvMergePath, string mp4MuxerPath)
        {
            _mkvMergePath = mkvMergePath;
            _mp4MuxerPath = mp4MuxerPath;
        }

        private Episode GenerateEpisode(
            List<string> inputFileNames,
            string outputFileName,
            string videoFps/* = "24000/1001"*/,
            string audioLanguage/* = "jpn"*/,
            string subtitleLanguage/* = "jpn"*/
        )
        {
            var episode = new Episode {
                AudioFiles = new List<string>(),
                SubtitleFiles = new List<string>(),
                VideoFps = videoFps,
                AudioLanguage = audioLanguage,
                SubtitleLanguage = subtitleLanguage,
                TotalFileSize = 0
            };

            foreach (string file in inputFileNames) {
                FileInfo fileInfo = new FileInfo(file);
                if (!fileInfo.Exists) {
                    continue;
                }

                var extension = Path.GetExtension(file).ToLower();

                if (!string.IsNullOrEmpty(s_AudioFileExtensions.Find(val => val == extension))) {
                    episode.AudioFiles.Add(file);
                    episode.TotalFileSize += fileInfo.Length;
                    continue;
                }
                if (!string.IsNullOrEmpty(s_VideoFileExtensions.Find(val => val == extension))) {
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

            foreach (var audioFile in episode.AudioFiles) {
                parameters.Add(string.Format(trackTemplate, episode.AudioLanguage, audioFile));
                trackOrder.Add($"{fileID++}:0");
            }

            if (episode.SubtitleFiles != null) {
                foreach (var subtitleFile in episode.SubtitleFiles) {
                    parameters.Add(string.Format(trackTemplate, episode.SubtitleLanguage, subtitleFile));
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

            parameters.Add($"-i {episode.VideoFile}?fps={episode.VideoFps}");

            foreach (var audioFile in episode.AudioFiles) {
                FileInfo ainfo = new FileInfo(audioFile);
                if (ainfo.Extension.ToLower() == ".aac" || ainfo.Extension.ToLower() == ".m4a") {
                    parameters.Add($"-i {audioFile}?language={episode.AudioLanguage}");
                }
            }

            parameters.Add($"-o {episode.OutputFile}");

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
                    int progress = 0;

                    switch (_episode.OutputFileType) {
                        case OutputType.Mkv:
                            if (line.Contains("Progress: ")) {
                                progressMatch = Regex.Match(line, @"Progress: (\d*?)%", RegexOptions.Compiled);
                                if (progressMatch.Groups.Count < 2) return;
                                progress = int.Parse(progressMatch.Groups[1].Value);
                            } else if (line.Contains("Muxing took")) {
                                mre.Set();
                            }
                            break;
                        case OutputType.Mp4:
                            if (line.Contains("Importing: ")) {
                                progressMatch = Regex.Match(line, @"Importing: (\d*?) bytes", RegexOptions.Compiled);
                                if (progressMatch.Groups.Count < 2) return;
                                progress = Convert.ToInt32(double.Parse(progressMatch.Groups[1].Value) / _episode.TotalFileSize * 100d);
                            } else if (line.Contains("Muxing completed")) {
                                mre.Set();
                            }
                            break;
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

        public void StartMerge(
            List<string> inputFileNames,
            string outputFileName,
            string videoFps = "24000/1001",
            string audioLanguage = "jpn",
            string subtitleLanguage = "jpn"
        )
        {
            _episode = GenerateEpisode(inputFileNames, outputFileName, videoFps, audioLanguage, subtitleLanguage);
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
    }
}
