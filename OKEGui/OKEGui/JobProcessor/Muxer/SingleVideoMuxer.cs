using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public class SingleVideoMuxer : MkvmergeMuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("SingleVideoMuxer");

        public SingleVideoMuxer(MuxJob mJob) : base(mJob)
        {
            BuildCommandline();
        }

        public override void BuildCommandline()
        {
            base.BuildCommandline();

            commandLine += " --no-audio --no-subtitles --no-buttons --no-track-tags --no-chapters --no-attachments --no-global-tags";

            commandLine += $" --default-track \"0:1\" --language 0:und \"(\" \"{MJob.Input}\" \")\"";
            if (MJob.IsPartialMux)
            {
                // OKE's frame number range is in python format, with the start frame included but the end frame excluded.
                // So when converting to mkvmerge format, only the start frame number is added by one.
                // According to mkvmerge's documentation, when splitting by frame, the result will contain the content up to but excluding the first key frame at or after the end frame.
                // It means that if the end frame happens to be a key frame (there are two consecutive key frames in end and end+1), the end frame won't be included.
                // To avoid such a situation, we pass end+1 to mkvmerge, since end+1 frame is always a key frame and it won't be included.
                commandLine += $" --split parts-frames:{MJob.FrameRange.begin + 1}-{MJob.FrameRange.end + 1}";
            }
        }
    }
}
