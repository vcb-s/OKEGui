using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public class AppendVideoMuxer : MkvmergeMuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("AppendVideoMuxer");

        public AppendVideoMuxer(MuxJob mJob) : base(mJob)
        {
            BuildCommandline();
        }

        public override void BuildCommandline()
        {
            base.BuildCommandline();

            if (!string.IsNullOrEmpty(MJob.TimeCodeFile))
                commandLine += $" --timestamps \"0:{MJob.TimeCodeFile}\"";

            commandLine += " --no-audio --no-subtitles --no-buttons --no-track-tags --no-chapters --no-attachments --no-global-tags";
            commandLine += " --default-track \"0:1\" --language 0:und";
            for (int i = 0; i < MJob.VideoSlices.Count; i++)
            {
                if (i == 0)
                    commandLine += $" \"(\" \"{MJob.VideoSlices[i].File.GetFullPath()}\" \")\"";
                else
                    commandLine += $" + \"(\" \"{MJob.VideoSlices[i].File.GetFullPath()}\" \")\"";
            }

            commandLine += " --append-to ";
            for (int i = 1; i < MJob.VideoSlices.Count; i++)
            {
                if (i == 1)
                    commandLine += $"{i}:0:{i-1}:0";
                else
                    commandLine += $",{i}:0:{i-1}:0";
            }
        }
    }
}
