using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    public abstract class MkvmergeMuxer : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("MkvmergeMuxer");

        protected MuxJob MJob
        {
            get { return job as MuxJob; }
        }

        public MkvmergeMuxer(MuxJob mJob) : base(mJob)
        {
            FileInfo mkvInfo = new FileInfo(Constants.mkvmergePath);
            if (!mkvInfo.Exists)
                throw new Exception("mkvmerge封装工具不存在");

            executable = mkvInfo.FullName;
        }

        public virtual void BuildCommandline()
        {
            commandLine = "--ui-language en";
            commandLine += $" --output \"{MJob.Output}\"";
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Logger.Debug(line);

            Regex rProgress = new Regex(@"Progress: (\d*?)%", RegexOptions.Compiled);
            double p = 0;

            if (line.Contains("Progress: "))
            {
                string[] match = rProgress.Split(line);
                double.TryParse(match[1], out p);
                if (p > 1)
                {
                    MJob.Progress = p;
                }
            }
            if (line.Contains("Muxing took") || line.Contains("Multiplexing took"))
            { //different versions of mkvmerge may return different wordings. Muxing took is the old way.
                MJob.Progress = 100;
                SetFinish();
            }
        }

        protected override void onExited(int exitCode)
        {
            if (exitCode != 0)
            {
                if (exitCode == 2)
                    Logger.Error("mkvmerge封装出错");
            }
        }
    }
}
