using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OKEGui.Utils;
using System.Collections.Generic;

namespace OKEGui.JobProcessor
{
    public class X265Encoder : CommandlineVideoEncoder
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("X265Encoder");
        private readonly string x265Path = "";
        private readonly string vspipePath = "";

        public X265Encoder(VideoJob vjob) : base(vjob)
        {
            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (!File.Exists(VJob.EncoderPath))
            {
                throw new Exception("x265编码器不存在");
            }

            x265Path = VJob.EncoderPath;
            vspipePath = Initializer.Config.vspipePath;

            commandLine = BuildCommandline();
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            if (line.Contains("x265 [error]:"))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.x265ErrorSmr);
                ex.progress = 0.0;
                ex.Data["X265_ERROR"] = line.Substring(14);
                throw ex;
            }

            if (line.Contains("Error: fwrite() call failed when writing frame: "))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.x265CrashSmr);
                throw ex;
            }

            if (line.ToLowerInvariant().Contains("encoded"))
            {
                Logger.Debug(line);
                Regex rf = new Regex("encoded ([0-9]+) frames in ([0-9]+.[0-9]+)s \\(([0-9]+.[0-9]+) fps\\), ([0-9]+.[0-9]+) kb/s, Avg QP:(([0-9]+.[0-9]+))");

                var result = rf.Split(line);

                long reportedFrames = long.Parse(result[1]);

                // 这里是平均速度
                if (!SetSpeed(result[3]))
                {
                    return;
                }

                Debugger.Log(0, "EncodeFinish", result[3] + "fps\n");

                EncodeFinish(reportedFrames);
            }

            Regex regOfficial = new Regex("([0-9]+) frames: ([0-9]+.[0-9]+) fps, ([0-9]+.[0-9]+) kb/s", RegexOptions.IgnoreCase);
            Regex regAsuna = new Regex("([0-9]+)/[0-9]+ frames, ([0-9]+.[0-9]+) fps, ([0-9]+.[0-9]+) kb/s", RegexOptions.IgnoreCase);

            string[] status;

            if (regOfficial.Split(line).Length >= 3)
            {
                status = regOfficial.Split(line);
            }
            else if (regAsuna.Split(line).Length >= 3)
            {
                status = regAsuna.Split(line);
            }
            else
            {
                Logger.Debug(line);
                return;
            }

            if (!SetFrameNumber(status[1], true))
            {
                return;
            }

            SetBitrate(status[3], "kb/s");

            if (!SetSpeed(status[2]))
            {
                return;
            }
        }

        private string BuildCommandline()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("/c \"start \"foo\" /b /wait ");
            if (!Initializer.Config.singleNuma)
            {
                sb.Append("/affinity 0xFFFFFFFFFFFFFFFF /node ");
                sb.Append(VJob.NumaNode.ToString());
            }
            // 构建vspipe参数
            sb.Append(" \"" + vspipePath + "\"");
            sb.Append(" --y4m");
            if (VJob.IsPartialEncode)
            {
                sb.Append($" -s {VJob.FrameRange.begin} -e {VJob.FrameRange.end - 1}");
            }
            foreach (string arg in VJob.VspipeArgs)
            {
                sb.Append($" --arg \"{arg}\"");
            }
            sb.Append(" \"" + VJob.Input + "\"");
            sb.Append(" - |");

            // 构建x265参数
            sb.Append(" \"" + x265Path + "\"");
            if (Initializer.Config.avx512 && !VJob.EncodeParam.ToLower().Contains("--asm"))
            {
                sb.Append(" --asm avx512");
            }
            sb.Append(" --y4m " + VJob.EncodeParam + " -o");
            sb.Append(" \"" + VJob.Output + "\" -");
            sb.Append("\"");

            return sb.ToString();
        }

    }
}
