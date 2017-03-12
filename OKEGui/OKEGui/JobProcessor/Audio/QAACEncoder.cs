using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OKEGui
{
    internal class QAACEncoder : CommandlineJobProcessor
    {
        public static IJobProcessor NewQAACEncoder(string QAACPath, Job j)
        {
            var qaac = new FileInfo(QAACPath);
            if (qaac.Exists) {
                if (j is AudioJob) {
                    return new QAACEncoder(qaac.FullName, j as AudioJob);
                }
            }

            return null;
        }

        private string commandLine;
        private ManualResetEvent retrieved = new ManualResetEvent(false);

        // TODO: 变更编码参数
        public QAACEncoder(string QAACPath, AudioJob j, int bitrate = 128) : base()
        {
            if (j.Input == "-") {
                // stdin
            }

            executable = QAACPath;
            commandLine = "-i -v " + bitrate + " -q 2 --no-delay -o " + j.Output + " " + j.Input;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Debugger.Log(0, "QAACEncoder", line);
            if (line.Contains(".done")) {
                SetFinish();
            }
        }

        public override void setup(Job job, StatusUpdate su)
        {
        }

        public override string Commandline
        {
            get {
                return commandLine;
            }
        }
    }
}
