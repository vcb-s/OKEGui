using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OKEGui.Utils;
using OKEGui.JobProcessor;

namespace OKEGui
{
    public class x265Encoder : CommandlineVideoEncoder
    {
        //  public static readonly JobProcessorFactory Factory = new JobProcessorFactory(new ProcessorFactory(init), "x265Encoder");

        private string commandLine = "";
        private string x265Path = "";
        private string vspipePath = "";

        public static IJobProcessor init(Job j, string extractParam)
        {
            if (j is VideoJob && ((j as VideoJob).CodecString == "X265" || (j as VideoJob).CodecString == "HEVC")) {
                return new x265Encoder((j as VideoJob), extractParam);
            }
            return null;
        }

        public x265Encoder(Job j, string extractParam)
            : base()
        {
            job = j as VideoJob;
            getInputProperties(job);

            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (File.Exists(job.EncoderPath)) {
                this.x265Path = job.EncoderPath;
            }

            // 获取VSPipe路径
            this.vspipePath = ConfigManager.Config.vspipePath;

            commandLine = BuildCommandline(extractParam);
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            //if (line.StartsWith("[")) // status update
            //{
            //    int frameNumberStart = line.IndexOf("]", 4) + 2;
            //    int frameNumberEnd = line.IndexOf("/");
            //    if (frameNumberStart > 0 && frameNumberEnd > 0 && frameNumberEnd > frameNumberStart)
            //        if (base.setFrameNumber(line.Substring(frameNumberStart, frameNumberEnd - frameNumberStart).Trim()))
            //            return;
            //}
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

            if (line.ToLowerInvariant().Contains("encoded")) {
                Regex rf = new Regex("encoded ([0-9]+) frames in ([0-9]+.[0-9]+)s \\(([0-9]+.[0-9]+) fps\\), ([0-9]+.[0-9]+) kb/s, Avg QP:(([0-9]+.[0-9]+))");
                var result = rf.Split(line);
                // 这里是平均速度
                if (!base.setSpeed(result[3])) {
                    return;
                }

                Debugger.Log(0, "EncodeFinish", result[3] + "fps\n");

                base.encodeFinish();
            }

            Regex r = new Regex("([0-9]+) frames: ([0-9]+.[0-9]+) fps, ([0-9]+.[0-9]+) kb/s", RegexOptions.IgnoreCase);

            var status = r.Split(line);
            if (status.Length < 3) {
                return;
            }

            if (!base.setFrameNumber(status[1], true)) {
                return;
            }

            base.setBitrate(status[3], "kb/s");

            if (!base.setSpeed(status[2])) {
                return;
            }

            base.ProcessLine(line, stream);
        }

        // TODO: 改为静态
        public /*static*/ string BuildCommandline(string extractParam)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("/c ");
            sb.Append("\"");
            // 构建vspipe参数
            sb.Append("\"" + vspipePath + "\"");
            sb.Append(" --y4m ");
            sb.Append("\"" + job.Input + "\"");
            sb.Append(" - | ");

            // 构建x265参数
            sb.Append("\"" + x265Path + "\"");
            sb.Append(" --y4m " + extractParam + " -o ");
            sb.Append("\"" + job.Output + "\" -");
            sb.Append("\"");

            return sb.ToString();
        }

        public override string Commandline
        {
            get {
                return commandLine;
            }
        }
    }
}
