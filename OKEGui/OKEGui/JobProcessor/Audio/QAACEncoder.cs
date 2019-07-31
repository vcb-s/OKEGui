using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using OKEGui.Utils;
using OKEGui.JobProcessor;

namespace OKEGui
{
    internal class QAACEncoder : CommandlineJobProcessor
    {
        private ManualResetEvent retrieved = new ManualResetEvent(false);

        // TODO: 变更编码参数
        public QAACEncoder(AudioJob j, int bitrate = Constants.QAACBitrate) : base()
        {
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
            Debugger.Log(0, "QAACEncoder", line + "\n");
            if (line.Contains(".done"))
            {
                SetFinish();
            }
            if (line.Contains("ERROR"))
            {
                throw new OKETaskException(Constants.qaacErrorSmr);
            }
        }

        public override void setup(Job job, StatusUpdate su)
        {
        }

    }
}
