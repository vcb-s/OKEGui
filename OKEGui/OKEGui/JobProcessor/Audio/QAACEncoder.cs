using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OKEGui.Utils;
using OKEGui.JobProcessor;

namespace OKEGui
{
    internal class QAACEncoder : CommandlineJobProcessor
    {
        private ManualResetEvent retrieved = new ManualResetEvent(false);
        private Action<double> _progressCallback;

        // TODO: 变更编码参数
        public QAACEncoder(AudioJob j, Action<double> progressCallback, int bitrate = Constants.QAACBitrate) : base()
        {
            _progressCallback = progressCallback;
            if (j.Input != "-")
            { //not from stdin, but an actual file
                j.Input = $"\"{j.Input}\"";
            }

            executable = Constants.QAACPath;
            commandLine = $"-i -v {bitrate} -q 2 --no-delay -o \"{j.Output}\" {j.Input}";
        }

        public QAACEncoder(string commandLine)
        {
            this.executable = Constants.QAACPath;
            this.commandLine = commandLine;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);
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
