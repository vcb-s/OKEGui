using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;
using System.Text;

namespace OKEGui.JobProcessor
{
    internal class QAACEncoder : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("QAACEncoder");

        protected AudioJob AJob
        {
            get { return job as AudioJob; }
        }

        public QAACEncoder(AudioJob ajob) : base(ajob)
        {
            executable = Constants.QAACPath;

            var sb = new StringBuilder("-i ");
            if (AJob.Info.Quality != null)
            {
                sb.Append($"-V {AJob.Info.Quality} ");
            }
            else
            {
                sb.Append($"-v {AJob.Info.Bitrate} ");
            }
            sb.Append($"-q 2 --no-delay -o \"{AJob.Output}\" \"{AJob.Input}\"");
            
            commandLine = sb.ToString();
        }

        public QAACEncoder(string commandLine) : base(null)
        {
            this.executable = Constants.QAACPath;
            this.commandLine = commandLine;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Regex rAnalyze = new Regex("\\[([0-9.]+)%\\]");
            double p = 0;
            if (rAnalyze.IsMatch(line))
            {
                double.TryParse(rAnalyze.Split(line)[1], out p);
                if (p > 1)
                {
                    AJob.Progress = p;
                }
            }
            else
            {
                Logger.Debug(line);
                if (line.Contains(".done"))
                {
                    SetFinish();
                }
                if (line.Contains("ERROR"))
                {
                    throw new OKETaskException(Constants.qaacErrorSmr);
                }
            }
        }
    }
}
