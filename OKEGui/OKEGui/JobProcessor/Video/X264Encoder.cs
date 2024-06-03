using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OKEGui.Utils;
using System.Collections.Generic;

namespace OKEGui.JobProcessor
{
    public class X264Encoder : CommandlineVideoEncoder
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("X264Encoder");
        private readonly string x264Path = "";
        private readonly string vspipePath = "";

        public X264Encoder(VideoJob vjob) : base(vjob)
        {
            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (!File.Exists(VJob.EncoderPath))
            {
                throw new Exception("x264编码器不存在");
            }

            x264Path = VJob.EncoderPath;
            vspipePath = Initializer.Config.vspipePath;

            commandLine = BuildCommandline();
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            if (line.Contains("x264 [error]:") || line.Contains("unknown option"))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.x264ErrorSmr);
                ex.Data["X264_ERROR"] = line;
                throw ex;
            }

            if (line.Contains("Error: fwrite() call failed when writing frame: "))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.x264CrashSmr);
                throw ex;
            }

            if (line.ToLowerInvariant().Contains("encoded"))
            {
                Logger.Debug(line);
                Regex rf = new Regex("encoded ([0-9]+) frames, ([0-9]+.[0-9]+) fps, ([0-9]+.[0-9]+) kb/s");

                var result = rf.Split(line);
                if (result.Length <= 2)
                {
                    return;
                }

                long reportedFrames = long.Parse(result[1]);

                // 这里是平均速度
                if (!SetSpeed(result[2]))
                {
                    return;
                }

                Logger.Debug($"EncodeFinish {result[2]} fps");

                EncodeFinish(reportedFrames);
                return;
            }

            Regex r = new Regex("([0-9]+) frames: ([0-9]+.[0-9]+) fps, ([0-9]+.[0-9]+) kb/s", RegexOptions.IgnoreCase);

            var status = r.Split(line);
            if (status.Length < 3)
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

            // 构建x264参数
            sb.Append(" \"" + x264Path + "\"");
            sb.Append(" --demuxer y4m " + VJob.EncodeParam + " -o");
            sb.Append(" \"" + VJob.Output + "\" -");
            sb.Append("\"");

            return sb.ToString();
        }

    }
}
