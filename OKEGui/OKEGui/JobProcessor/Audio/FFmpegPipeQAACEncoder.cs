using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;
using OKEGui.JobProcessor;

namespace OKEGui
{
    internal class FFmpegPipeQAACEncoder : CommandlineJobProcessor
    {
        private ManualResetEvent retrieved = new ManualResetEvent(false);
        private Action<double> _progressCallback;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("FFmpegPipeQAACEncoder");
        private readonly int audioLength;

        // TODO: 变更编码参数
        public FFmpegPipeQAACEncoder(AudioJob j, Action<double> progressCallback, int bitrate = Constants.QAACBitrate) : base()
        {
            _progressCallback = progressCallback;

            this.audioLength = j.Info.Length;

            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            FileInfo ffmpegPath = new FileInfo(Constants.ffmpegPath);
            FileInfo QAACPath = new FileInfo(Constants.QAACPath);

            commandLine = "/c \"start \"qaacenc\" /b /wait ";
            commandLine += $"\"{ffmpegPath.FullName}\" -i \"{j.Input}\" -vn -sn -dn -f wav -v warning -";
            commandLine += " | ";
            commandLine += $"\"{QAACPath.FullName}\" -i -v {bitrate} -q 2 --no-delay --threading -o \"{j.Output}\" -";
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
                p = (hour * 3600 + minute * 60 + second) * 1.0 / this.audioLength * 100;
                Logger.Trace($"{hour * 3600 + minute * 60 + second}, {this.audioLength}, {p}\n");
                if (p > 1)
                {
                    _progressCallback(p);
                }
            }
            else if (rAnalyze2.IsMatch(line))
            {
                string[] match = rAnalyze2.Split(line);
                int minute = int.Parse(match[1]);
                int second = int.Parse(match[2]);
                p = (minute * 60 + second) * 1.0 / this.audioLength * 100;
                Logger.Trace($"{minute * 60 + second}, {this.audioLength}, {p}\n");
                if (p > 1)
                {
                    _progressCallback(p);
                }
            }
            else
            {
                Logger.Debug(line);
                if (line.Contains(".done"))
                {
                    _progressCallback(100);
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
