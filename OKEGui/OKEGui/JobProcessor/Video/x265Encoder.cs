using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OKEGui.Utils;
using OKEGui.JobProcessor;

namespace OKEGui
{
    public class X265Encoder : CommandlineVideoEncoder
    {
        private readonly string X265Path = "";
        private readonly string vspipePath = "";

        public X265Encoder(Job j) : base()
        {
            job = j as VideoJob;
            getInputProperties(job);

            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (File.Exists(job.EncoderPath))
            {
                this.X265Path = job.EncoderPath;
            }

            // 获取VSPipe路径
            this.vspipePath = Initializer.Config.vspipePath;

            commandLine = BuildCommandline(job.EncodeParam, job.NumaNode);
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);

            if (line.Contains("x265 [error]:"))
            {
                OKETaskException ex = new OKETaskException(Constants.x265ErrorSmr);
                ex.progress = 0.0;
                ex.Data["X265_ERROR"] = line.Substring(14);
                throw ex;
            }

            if (line.Contains("Error: fwrite() call failed when writing frame: "))
            {
                OKETaskException ex = new OKETaskException(Constants.x265CrashSmr);
                throw ex;
            }

            if (line.ToLowerInvariant().Contains("encoded"))
            {
                Regex rf = new Regex("encoded ([0-9]+) frames in ([0-9]+.[0-9]+)s \\(([0-9]+.[0-9]+) fps\\), ([0-9]+.[0-9]+) kb/s, Avg QP:(([0-9]+.[0-9]+))");

                var result = rf.Split(line);

                ulong reportedFrames = ulong.Parse(result[1]);

                // 这里是平均速度
                if (!base.setSpeed(result[3]))
                {
                    return;
                }

                Debugger.Log(0, "EncodeFinish", result[3] + "fps\n");

                base.encodeFinish(reportedFrames);
            }

            Regex r = new Regex("([0-9]+) frames: ([0-9]+.[0-9]+) fps, ([0-9]+.[0-9]+) kb/s", RegexOptions.IgnoreCase);

            var status = r.Split(line);
            if (status.Length < 3)
            {
                return;
            }

            if (!base.setFrameNumber(status[1], true))
            {
                return;
            }

            base.setBitrate(status[3], "kb/s");

            if (!base.setSpeed(status[2]))
            {
                return;
            }
        }

        private string BuildCommandline(string extractParam, int numaNode)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("/c \"start \"foo\" /b /wait /affinity 0xFFFFFFF /node ");
            sb.Append(numaNode.ToString());
            // 构建vspipe参数
            sb.Append(" \"" + vspipePath + "\"");
            sb.Append(" --y4m ");
            sb.Append("\"" + job.Input + "\"");
            sb.Append(" - | ");

            // 构建x265参数
            sb.Append("\"" + X265Path + "\"");
            sb.Append(" --y4m " + extractParam + " -o ");
            sb.Append("\"" + job.Output + "\" -");
            sb.Append("\"");

            return sb.ToString();
        }

    }
}
