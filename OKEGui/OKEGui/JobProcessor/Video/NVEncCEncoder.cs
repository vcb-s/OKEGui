using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OKEGui.Utils;
using System.Collections.Generic;

namespace OKEGui.JobProcessor
{
    public class NVEncCEncoder : CommandlineVideoEncoder
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("NVEncCEncoder");
        private readonly string nvenccPath = "";
        private readonly string vspipePath = "";

        public NVEncCEncoder(VideoJob vjob) : base(vjob)
        {
            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (!File.Exists(VJob.EncoderPath))
            {
                throw new Exception("NVEncC编码器不存在");
            }

            nvenccPath = VJob.EncoderPath;
            vspipePath = Initializer.Config.vspipePath;

            commandLine = BuildCommandline();
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            // 进度行以\r刷新且可能粘连，逐段处理
            foreach (string segment in line.Split('\r'))
            {
                ProcessSegment(segment.Trim());
            }
        }

        private void ProcessSegment(string line)
        {
            if (line.Length == 0)
            {
                return;
            }

            if (line.Contains("[error]") || line.Contains("Error: ") || line.ToLowerInvariant().Contains("unknown option")
                || line.Contains("failed to parse y4m header") || line.Contains("failed to initialize file reader"))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.nvenccErrorSmr);
                ex.Data["NVENCC_ERROR"] = line;
                throw ex;
            }

            if (line.Contains("Error: fwrite() call failed when writing frame: "))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.nvenccCrashSmr);
                throw ex;
            }

            if (line.ToLowerInvariant().Contains("encoded"))
            {
                Logger.Debug(line);
                Regex rf = new Regex(@"encoded ([0-9]+) frames, ([0-9]+\.[0-9]+) fps, ([0-9]+\.[0-9]+) kb(?:p|/)?s");

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

            // 两种进度格式：管道输入（总帧数未知）时为"46 frames: 36.74 fps, 5089 kbps, ..."，
            // 直接读文件时为"[82.2%] 77/400 frames: 231.23 fps, 533 kbps, ..."
            Regex r = new Regex(@"(?:\[ *[0-9]+\.[0-9]+%\] *)?([0-9]+)(?:/[0-9]+)? frames: *([0-9]+\.[0-9]+) fps, *([0-9]+(?:\.[0-9]+)?) kb(?:p|/)?s", RegexOptions.IgnoreCase);

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

            // 构建NVEncC参数，--y4m与-i -由程序注入，EncoderParam里放用户参数
            sb.Append(" \"" + nvenccPath + "\"");
            sb.Append(" --y4m -i - " + VJob.EncodeParam + " -o");
            sb.Append(" \"" + VJob.Output + "\"");
            sb.Append("\"");

            return sb.ToString();
        }

    }
}
