using System;
using System.IO;
using System.Threading;

namespace OKEGui
{
    class FLACDecoder : CommandlineJobProcessor
    {
        public static IJobProcessor NewFLACDecoder(string FlacPath, Job j)
        {
            var flac = new FileInfo(FlacPath);
            if (flac.Exists) {
                if (j is AudioJob) {
                    return new FLACDecoder(flac.FullName, j as AudioJob);
                }
            }

            return null;
        }

        private string commandLine;
        private ManualResetEvent retrieved = new ManualResetEvent(false);

        // TODO: 变更编码参数
        public FLACDecoder(string FlacPath, AudioJob j) : base()
        {
            commandLine = "-d ";
            if (j.Output == "-") {
                commandLine += "--stdout ";
            } else if (j.Output != "") {
                commandLine += "-o " + j.Output;
            }

            if (Path.GetExtension(j.Input) == ".flac") {
                commandLine += j.Input;
            }

            executable = FlacPath;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            if (line.Contains("done")) {
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
