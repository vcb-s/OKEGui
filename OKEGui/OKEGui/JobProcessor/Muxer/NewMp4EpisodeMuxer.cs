using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Model;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public class NewMp4EpisodeMuxer : LSmashMuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("NewMp4EpisodeMuxer");

        public NewMp4EpisodeMuxer(MuxJob mJob) : base(mJob)
        {
            BuildCommandline();
        }

        public override void BuildCommandline()
        {
            base.BuildCommandline();

            List<string> AcceptableAudioExtensions = new List<string> {
                ".aac", ".m4a", ".ac3", ".dts", ".eac3"
            };

            if (MJob.MediaOutFile.VideoTrack != null)
            {
                var VideoTrack = MJob.MediaOutFile.VideoTrack;
                var VideoInfo = MJob.MediaOutFile.VideoTrack.Info as VideoInfo;
                commandLine += $" -i \"{VideoTrack.File.GetFullPath()}\"?fps={VideoInfo.FpsNum}/{VideoInfo.FpsDen}";
            }

            if (MJob.MediaOutFile.AudioTracks != null)
            {
                var AudioTracks = MJob.MediaOutFile.AudioTracks.OrderBy(trk => trk.Info.Order).ToList();
                for (int i = 0; i < AudioTracks.Count; i++)
                {
                    string audioExtension = AudioTracks[i].File.GetExtension().ToLower();
                    if (AcceptableAudioExtensions.Contains(audioExtension))
                    {
                        commandLine += $" -i \"{AudioTracks[i].File.GetFullPath()}\"?language={AudioTracks[i].Info.Language},handler=\"{AudioTracks[i].Info.Name}\"";
                    }
                    else
                    {
                        Logger.Warn($"MP4不支持封装[{audioExtension}]格式音轨: {AudioTracks[i].File.GetFullPath()}");
                    }
                }
            }

            if (MJob.MediaOutFile.ChapterTrack != null)
            {
                commandLine += $" --chapter \"{MJob.MediaOutFile.ChapterTrack.File.GetFullPath()}\"";
            }
        }
    }
}
