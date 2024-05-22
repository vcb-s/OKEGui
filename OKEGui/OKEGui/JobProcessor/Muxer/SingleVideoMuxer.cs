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
                commandLine += $" --split parts-frames:{MJob.FrameRange.begin + 1}-{MJob.FrameRange.end}";
            }
        }
    }
}
