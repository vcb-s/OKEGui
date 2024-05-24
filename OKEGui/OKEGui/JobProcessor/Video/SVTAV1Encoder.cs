using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OKEGui.Utils;
using System.Collections.Generic;

namespace OKEGui.JobProcessor
{
    public class SVTAV1Encoder : CommandlineVideoEncoder
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("SVTAV1Encoder");
        private readonly string svtav1Path = "";
        private readonly string vspipePath = "";
        private bool expectTotalFrames = false;

        public SVTAV1Encoder(VideoJob vjob) : base(vjob)
        {
            executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (!File.Exists(VJob.EncoderPath))
            {
                throw new Exception("svtav1编码器不存在");
            }

            svtav1Path = VJob.EncoderPath;
            vspipePath = Initializer.Config.vspipePath;

            commandLine = BuildCommandline();
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            if (line.Contains("Svt[error]: ") || line.Contains("[SVT-Error]: ") || line.Contains("Error: "))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.svtav1ErrorSmr);
                ex.progress = 0.0;
                ex.Data["SVTAV1_ERROR"] = line.Substring(line.IndexOf(':')+2);
                throw ex;
            }

            if (line.Contains("Error: fwrite() call failed when writing frame: "))
            {
                Logger.Error(line);
                OKETaskException ex = new OKETaskException(Constants.svtav1CrashSmr);
                throw ex;
            }

            if (line.ToLowerInvariant().Contains("all_done_encoding")) // svt-av1 must be built with -DLOG_ENC_DONE=1.
            {
                Logger.Debug(line);
                Regex rf = new Regex("all_done_encoding *([0-9]+) frames");

                var result = rf.Split(line);

                long reportedFrames = long.Parse(result[1]);

                Debugger.Log(0, "EncodeFinish", result[1] + " frames\n");

                EncodeFinish(reportedFrames);
            }
            if (line.StartsWith("Total Frames\t"))
            {
                Logger.Debug(line);
                expectTotalFrames = true;
                return;
            }
            if (expectTotalFrames)
            {
                Logger.Debug(line);
                Regex rf = new Regex("[\t ]*([0-9]+)[\t ]");
                var result = rf.Split(line);
                if (result.Length < 2)
                    return;
                long reportedFrames = long.Parse(result[1]);
                Debugger.Log(0, "EncodeFinish", result[1] + " frames\n");
                EncodeFinish(reportedFrames);
                expectTotalFrames = false;
            }

            Regex regOfficial = new Regex("Encoding frame *([0-9]+) *([0-9]+.[0-9]+) *kbps *([0-9]+.[0-9]+) *(fp[sm])", RegexOptions.IgnoreCase);

            string[] status;

            if (regOfficial.Split(line).Length >= 4)
            {
                status = regOfficial.Split(line);
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

            SetBitrate(status[2], "kb/s");

            if (!SetSpeed(status[3], status[4]))
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

            // 构建svtav1参数
            sb.Append(" \"" + svtav1Path + "\"");
            sb.Append(" --progress 2 " + VJob.EncodeParam + " -b");
            sb.Append(" \"" + VJob.Output + "\" -i -");
            sb.Append("  \""); // leave extra spaces for ApppendParameter.

            return sb.ToString();
        }

    }
}
