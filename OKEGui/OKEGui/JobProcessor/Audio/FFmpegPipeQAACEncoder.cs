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

        // TODO: 变更编码参数
        public FFmpegPipeQAACEncoder(AudioJob j, Action<double> progressCallback, int bitrate = Constants.QAACBitrate) : base()
        {
            _progressCallback = progressCallback;

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
            Regex rAnalyze = new Regex("\\[([0-9.]+)%\\]");
            double p = 0;
            if (rAnalyze.IsMatch(line))
            {
                double.TryParse(rAnalyze.Split(line)[1], out p);
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
