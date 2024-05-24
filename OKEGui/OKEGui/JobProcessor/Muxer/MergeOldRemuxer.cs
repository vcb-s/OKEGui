using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public class MergeOldRemuxer : MkvmergeMuxer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("MergeOldRemuxer");

        public MergeOldRemuxer(MuxJob mJob) : base(mJob)
        {
            BuildCommandline();
        }

        public override void BuildCommandline()
        {
            base.BuildCommandline();

            commandLine += $" --no-video \"(\" \"{MJob.ReEncodeOldFile}\" \")\"";

            if (!string.IsNullOrEmpty(MJob.TimeCodeFile))
                commandLine += $" --timestamps \"0:{MJob.TimeCodeFile}\"";

            commandLine += " --default-track \"0:1\" --language 0:und";
            commandLine += $" \"(\" \"{MJob.Input}\" \")\"";

            commandLine += " --track-order 1:0";
        }
    }
}
