using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Model;

namespace OKEGui
{
    public class AutoMuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private enum OutputType
        {
            Mkv,
            Mp4
        }

        private struct Episode
        {
            public string VideoFile;
            public string VideoFps;
            public string VideoName;
            public string timeCodeFile;

            public List<string> AudioFiles;
            public List<string> AudioLanguages;
            public List<string> AudioNames;

            public string ChapterFile;

            public List<string> SubtitleFiles;
            public List<string> SubtitleLanguages;
            public List<string> SubtitleNames;

            public OutputType OutputFileType;
            public string OutputFile;

            public long TotalFileSize;
        }

        private static List<string> s_AudioFileExtensions = new List<string> {
            ".flac", ".wav", ".ac3", ".dts", ".aac", ".m4a"
        };

        private static List<string> s_VideoFileExtensions = new List<string> {
            ".hevc", ".h265", ".avc", ".h264", ".mkv"
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
            string videoName/* = ""*/,
            string timeCodeFile/* = null */,
            List<string> audioLanguages/* = "jpn"*/,
            List<string> audioNames/* = ""*/,
            List<string> subtitleLanguages/* = "jpn"*/,
            List<string> subtitleNames/* = ""*/
        )
        {
            var episode = new Episode
            {
                AudioFiles = new List<string>(),
                SubtitleFiles = new List<string>(),
                VideoFps = videoFps,
                VideoName = videoName,
                timeCodeFile = timeCodeFile,
                AudioLanguages = audioLanguages,
                AudioNames = audioNames,
                SubtitleLanguages = subtitleLanguages,
                SubtitleNames = subtitleNames,
                TotalFileSize = 0
            };

            foreach (string file in inputFileNames)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (!fileInfo.Exists)
                {
                    continue;
                }

                var extension = Path.GetExtension(file).ToLower();

                if (s_AudioFileExtensions.Contains(extension))
                {
                    episode.AudioFiles.Add(file);
                    episode.TotalFileSize += fileInfo.Length;
                    continue;
                }
                if (s_VideoFileExtensions.Contains(extension))
                {
                    episode.VideoFile = file;
                    episode.TotalFileSize += fileInfo.Length;
                    continue;
                }
                switch (extension)
                {
                    case ".txt":
                        episode.ChapterFile = file;
                        break;

                    case ".sup":
                        episode.SubtitleFiles.Add(file);
                        break;
                }
            }

            episode.OutputFile = outputFileName;
            switch (Path.GetExtension(outputFileName).ToLower())
            {
                case ".mkv":
                case ".mka":
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
            string trackTemplate = "--language 0:{0} --track-name \"0:{1}\" \"(\" \"{2}\" \")\"";

            var parameters = new List<string>();
            var trackOrder = new List<string>();

            int fileID = 0;

            // parameters.Add("--ui-language zh_CN");
            parameters.Add($"--output \"{episode.OutputFile}\"");

            if (episode.VideoFile != null)
            {
                if (!string.IsNullOrEmpty(episode.timeCodeFile))
                {
                    parameters.Add($"--timestamps \"0:{episode.timeCodeFile}\"");
                }
                parameters.Add(string.Format(trackTemplate, "und", episode.VideoName, episode.VideoFile));
                trackOrder.Add($"{fileID++}:0");
            }

            for (int i = 0; i < episode.AudioFiles.Count; i++)
            {
                string audioFile = episode.AudioFiles[i];
                parameters.Add(string.Format(trackTemplate, episode.AudioLanguages[i], episode.AudioNames[i], audioFile));
                trackOrder.Add($"{fileID++}:0");
            }

            if (episode.SubtitleFiles != null)
            {
                for (int i = 0; i < episode.SubtitleFiles.Count; i++)
                {
                    string subtitleFile = episode.SubtitleFiles[i];
                    parameters.Add(string.Format(trackTemplate, episode.SubtitleLanguages[i], episode.SubtitleNames[i], subtitleFile));
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

            parameters.Add($"-i \"{episode.VideoFile}\"?fps={episode.VideoFps},handler=\"{episode.VideoName}\"");

            for (int i = 0; i < episode.AudioFiles.Count; i++)
            {
                string audioFile = episode.AudioFiles[i];
                FileInfo ainfo = new FileInfo(audioFile);
                if (ainfo.Extension.ToLower() == ".aac" || ainfo.Extension.ToLower() == ".m4a" || ainfo.Extension.ToLower() == ".ac3" || ainfo.Extension.ToLower() == ".dts")
                {
                    parameters.Add($"-i \"{audioFile}\"?language={episode.AudioLanguages[i]},handler=\"{episode.AudioNames[i]}\"");
                }
            }

            parameters.Add($"-o \"{episode.OutputFile}\"");

            return string.Join(" ", parameters);
        }

        private void StartProcess(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            proc.StartInfo = startInfo;
            Logger.Info(filename + " " + arguments);

            try
            {
                proc.Start();
                new Thread(new ThreadStart(readStdErr)).Start();
                new Thread(new ThreadStart(readStdOut)).Start();
                proc.WaitForExit();
                mre.WaitOne();
            }
            catch (Exception e) { throw e; }
        }

        private void readStream(StreamReader sr)
        {
            string line;
            if (null == proc) return;

            while ((line = sr.ReadLine()) != null)
            {
                Logger.Trace(line);

                Match progressMatch;
                double progress = -1;

                switch (_episode.OutputFileType)
                {
                    case OutputType.Mkv:
                        if (line.Contains("Progress: "))
                        {
                            progressMatch = Regex.Match(line, @"Progress: (\d*?)%", RegexOptions.Compiled);
                            if (progressMatch.Groups.Count < 2) return;
                            progress = double.Parse(progressMatch.Groups[1].Value);
                        }
                        else if (line.Contains("Muxing took") || line.Contains("Multiplexing took"))
                        { //different versions of mkvmerge may return different wordings. Muxing took is the old way.
                            mre.Set();
                        }
                        break;

                    case OutputType.Mp4:
                        if (line.Contains("Importing: "))
                        {
                            progressMatch = Regex.Match(line, @"Importing: (\d*?) bytes", RegexOptions.Compiled);
                            if (progressMatch.Groups.Count < 2) return;
                            progress = Convert.ToDouble(double.Parse(progressMatch.Groups[1].Value) / _episode.TotalFileSize * 100d);
                        }
                        else if (line.Contains("Muxing completed"))
                        {
                            mre.Set();
                        }
                        break;
                }
                if (progress > -1 && ProgressChanged != null)
                {
                    ProgressChanged(progress);
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

        public void StartMerge(
            List<string> inputFileNames,
            string outputFileName,
            string videoFps,
            string videoName,
            string timeCodeFile,
            List<string> audioLanguages,
            List<string> audioNames,
            List<string> subtitleLanguages,
            List<string> subtitleNames
        )
        {
            _episode = GenerateEpisode(inputFileNames, outputFileName, videoFps, videoName, timeCodeFile, audioLanguages, audioNames, subtitleLanguages, subtitleNames);
            string mainProgram = string.Empty;
            string args = string.Empty;
            switch (_episode.OutputFileType)
            {
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

        public OKEFile StartMuxing(string path, MediaFile mediaFile)
        {
            List<string> input = new List<string>();
            string videoFps = "";
            string videoName = "";
            string timeCodeFile = null;
            List<string> audioLanguages = new List<string>();
            List<string> audioNames = new List<string>();
            List<string> subtitleLanguages = new List<string>();
            List<string> subtitleNames = new List<string>();

            foreach (var track in mediaFile.Tracks)
            {
                if (track.Info.MuxOption != MuxOption.Default && track.Info.MuxOption != MuxOption.Mka)
                {
                    continue;
                }
                switch (track.TrackType)
                {
                    case TrackType.Audio:
                        AudioTrack audioTrack = track as AudioTrack;
                        audioLanguages.Add(audioTrack.Info.Language);
                        audioNames.Add(audioTrack.Info.Name);
                        break;
                    case TrackType.Video:
                        VideoTrack videoTrack = track as VideoTrack;
                        VideoInfo videoInfo = track.Info as VideoInfo;
                        videoFps = $"{videoInfo.FpsNum}/{videoInfo.FpsDen}";
                        videoName = videoInfo.Name;
                        timeCodeFile = videoInfo.TimeCodeFile;
                        break;
                    case TrackType.Subtitle:
                        SubtitleTrack subtitleTrack = track as SubtitleTrack;
                        subtitleLanguages.Add(subtitleTrack.Info.Language);
                        subtitleNames.Add(subtitleTrack.Info.Name);
                        break;
                }

                input.Add(track.File.GetFullPath());
            }

            this.StartMerge(input, path, videoFps, videoName, timeCodeFile, audioLanguages, audioNames, subtitleLanguages, subtitleNames);

            OKEFile outFile = new OKEFile(path);
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
