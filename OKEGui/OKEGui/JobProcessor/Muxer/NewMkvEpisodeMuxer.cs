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
    public class NewMkvEpisodeMuxer : MkvmergeMuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("NewMkvEpisodeMuxer");

        public NewMkvEpisodeMuxer(MuxJob mJob) : base(mJob)
        {
            BuildCommandline();
        }

        public override void BuildCommandline()
        {
            base.BuildCommandline();

            const string trackTemplate = " --default-track \"0:{0}\" --language 0:{1} --track-name \"0:{2}\" \"(\" \"{3}\" \")\"";
            List<string> trackOrder = new List<string>();
            int fileID = 0;

            if (MJob.MediaOutFile.VideoTrack != null)
            {
                string timeCodeFile = (MJob.MediaOutFile.VideoTrack.Info as VideoInfo).TimeCodeFile;
                if (!string.IsNullOrEmpty(timeCodeFile))
                {
                    commandLine += $" --timestamps \"0:{timeCodeFile}\"";
                }
                commandLine += string.Format(trackTemplate, "1", "und", null, MJob.MediaOutFile.VideoTrack.File.GetFullPath());
                trackOrder.Add($"{fileID++}:0");
            }

            if (MJob.MediaOutFile.AudioTracks != null)
            {
                var AudioTracks = MJob.MediaOutFile.AudioTracks.OrderBy(trk => trk.Info.Order).ToList();
                for (int i = 0; i < AudioTracks.Count; i++)
                {
                    bool isDefault = MJob.MediaOutFile.VideoTrack != null && i == 0;
                    commandLine += string.Format(trackTemplate, isDefault ? "1":"0", AudioTracks[i].Info.Language, AudioTracks[i].Info.Name, AudioTracks[i].File.GetFullPath());
                    trackOrder.Add($"{fileID++}:0");
                }
            }

            if (MJob.MediaOutFile.SubtitleTracks != null)
            {
                var SubTracks = MJob.MediaOutFile.SubtitleTracks.OrderBy(trk => trk.Info.Order).ToList();
                for (int i = 0; i < SubTracks.Count; i++)
                {
                    commandLine += string.Format(trackTemplate, "0", SubTracks[i].Info.Language, SubTracks[i].Info.Name, SubTracks[i].File.GetFullPath());
                    trackOrder.Add($"{fileID++}:0");
                }
            }

            if (MJob.MediaOutFile.ChapterTrack != null)
            {
                if (!string.IsNullOrEmpty(MJob.MediaOutFile.ChapterTrack.Info.Language))
                    commandLine += $" --chapter-language \"{MJob.MediaOutFile.ChapterTrack.Info.Language}\"";
                commandLine += $" --chapters \"{MJob.MediaOutFile.ChapterTrack.File.GetFullPath()}\"";
            }

            commandLine += string.Format(" --track-order {0}", string.Join(",", trackOrder));
        }
    }
}
