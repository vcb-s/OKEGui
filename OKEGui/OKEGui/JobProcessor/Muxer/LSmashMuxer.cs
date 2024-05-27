using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public abstract class LSmashMuxer : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("LSmashMuxer");

        protected MuxJob MJob
        {
            get { return job as MuxJob; }
        }

        public LSmashMuxer(MuxJob mJob) : base(mJob)
        {
            FileInfo lsmashInfo = new FileInfo(Constants.lsmashPath);
            if (!lsmashInfo.Exists)
                throw new Exception("l-smash封装工具不存在");

            executable = lsmashInfo.FullName;
        }

        public virtual void BuildCommandline()
        {
            commandLine = "--file-format mp4";
            commandLine += $" -o \"{MJob.Output}\"";
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Logger.Debug(line);

            Regex rProgress = new Regex(@"Importing: (\d*?) bytes", RegexOptions.Compiled);
            double p = 0;

            if (line.Contains("Error: "))
            {
                OKETaskException ex = new OKETaskException(Constants.lsmashErrorSmr);
                ex.Data["LSMASH_ERROR"] = line.Substring(7);
                throw ex;
            }
            if (line.Contains("Importing: "))
            {
                string[] match = rProgress.Split(line);
                double.TryParse(match[1], out double size);
                p = size / MJob.TotalFileSize * 100d;
                if (p > 1)
                {
                    MJob.Progress = p;
                }
            }
            if (line.Contains("Muxing completed"))
            {
                MJob.Progress = 100;
                SetFinish();
            }
        }

        protected override void onExited(int exitCode)
        {
            if (exitCode != 0)
            {
                Logger.Error("l-smash封装出错");
            }
        }
    }
}
