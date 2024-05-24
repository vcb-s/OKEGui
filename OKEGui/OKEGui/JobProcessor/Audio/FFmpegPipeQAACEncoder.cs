using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
{
    internal class FFmpegPipeQAACEncoder : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("FFmpegPipeQAACEncoder");

        protected AudioJob AJob
        {
            get { return job as AudioJob; }
        }

        public FFmpegPipeQAACEncoder(AudioJob ajob) : base(ajob)
        {
            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            FileInfo ffmpegPath = new FileInfo(Constants.ffmpegPath);
            FileInfo QAACPath = new FileInfo(Constants.QAACPath);

            commandLine = "/c \"start \"qaacenc\" /b /wait ";
            commandLine += $"\"{ffmpegPath.FullName}\" -i \"{AJob.Input}\" -vn -sn -dn -f wav -v warning -";
            commandLine += " | ";
            commandLine += $"\"{QAACPath.FullName}\" -i -v {AJob.Info.Bitrate} -q 2 --no-delay --threading -o \"{AJob.Output}\" -";
            commandLine += "\"";
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            // hh:mm:ss.mss
            Regex rAnalyze1 = new Regex(@"(\d*):(\d*):(\d*).(\d*) \(");
            // mm:ss.mss
            Regex rAnalyze2 = new Regex(@"(\d*):(\d*).(\d*) \(");
            double p = 0;
            if (rAnalyze1.IsMatch(line))
            {
                string[] match = rAnalyze1.Split(line);
                int hour = int.Parse(match[1]);
                int minute = int.Parse(match[2]);
                int second = int.Parse(match[3]);
                p = (hour * 3600 + minute * 60 + second) * 1.0 / AJob.Info.Length * 100;
                Logger.Trace($"{hour * 3600 + minute * 60 + second}, {AJob.Info.Length}, {p}\n");
                if (p > 1)
                {
                    AJob.Progress = p;
                }
            }
            else if (rAnalyze2.IsMatch(line))
            {
                string[] match = rAnalyze2.Split(line);
                int minute = int.Parse(match[1]);
                int second = int.Parse(match[2]);
                p = (minute * 60 + second) * 1.0 / AJob.Info.Length * 100;
                Logger.Trace($"{minute * 60 + second}, {AJob.Info.Length}, {p}\n");
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
                    AJob.Progress = 100;
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
