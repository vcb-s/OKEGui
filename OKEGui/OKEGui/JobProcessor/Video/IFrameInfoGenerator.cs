using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using OKEGui.JobProcessor;
using OKEGui.Utils;
using OKEGui.Model;

namespace OKEGui
{
    public class IFrameInfoGenerator : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("IFrameInfoGenerator");
        private ManualResetEvent retrieved = new ManualResetEvent(false);
        private IFrameInfo iFrameInfo;
        private long numFrames;
        private bool infoOk;
        private string lastStderrLine;
        private bool isVSError = false;
        private string errorMsg;

        public IFrameInfoGenerator(VideoInfoJob j) : base()
        {
            // 获取VSPipe路径
            executable = Initializer.Config.vspipePath;
            iFrameInfo = new IFrameInfo();
            infoOk = false;
            numFrames = j.NumberOfFrames;

            string vpyFile = PrepareScript(j);

            StringBuilder sb = new StringBuilder();

            sb.Append("--info");
            sb.Append($" \"{vpyFile}\"");

            commandLine = sb.ToString();
        }

        private string PrepareScript(VideoInfoJob j)
        {
            string vpyContent = "import sys\n" +
                                "from vapoursynth import core\n" +
                                $"a=R\"{j.ReEncodeOldFile}\"\n" +
                                "src = core.lsmas.LWLibavSource(a, cache=0, framelist=True)\n" +
                                "src.text.FrameProps(\"_IFrameList\").set_output(0)\n" +
                                "print(f\"IFrameList: {src.get_frame(0).props._IFrameList}\", file=sys.stderr)\n";
            string vpyFileName = j.WorkingPath + "_iframe.vpy";
            File.WriteAllText(vpyFileName, vpyContent);

            return vpyFileName;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Logger.Debug(line);
            if (stream == StreamType.Stderr)
                lastStderrLine = line;
            Regex rIFrame = new Regex("IFrameList: \\[([0-9, ]+)\\]");
            Regex rFrames = new Regex("Frames: ([0-9]+)");

            if (line.Contains("Python exception: "))
            {
                isVSError = true;
                errorMsg = "";
            }
            else if (isVSError)
            {
                Regex rExit = new Regex("^([a-zA-Z]*)(Error|Exception|Exit|Interrupt|Iteration|Warning)");
                if (rExit.IsMatch(line))
                {
                    string[] match = rExit.Split(line);
                    Logger.Error(match[1] + match[2]);

                    errorMsg += "\n" + line;
                    OKETaskException ex = new OKETaskException(Constants.vpyErrorSmr);
                    ex.Data["VPY_ERROR"] = errorMsg;
                    throw ex;
                }
                else if (line != "")
                {
                    errorMsg += "\n" + line;
                }
            }
            else if (line.Contains("Frames"))
            {
                var s = rFrames.Split(line);
                long f;
                long.TryParse(s[1], out f);

                // 旧版压制成品的帧数应该 <= 压制脚本最终输出帧数
                if (f > numFrames)
                {
                    OKETaskException ex = new OKETaskException(Constants.reEncodeFramesErrorSmr);
                    ex.Data["VPY_FRAMES"] = $"{numFrames}";
                    ex.Data["OLD_FRAMES"] = $"{f}";
                    throw ex;
                }

                Logger.Debug($"旧版压制成品帧数：{f}");
                if (f != numFrames)
                {
                    Logger.Warn($"旧版压制成品帧数({f})与压制脚本输出帧数({numFrames})不匹配，这不是ReEncode的标准用法，除非你非常清楚自己在做什么");
                }

                // 假设到这里已经获取完毕了
                infoOk = true;
                retrieved.Set();
            }
            else if (line.Contains("IFrameList"))
            {
                var split = rIFrame.Split(line);
                foreach (var s in split[1].Split(new string[] {",", " "}, StringSplitOptions.RemoveEmptyEntries))
                {
                    long w = -1;
                    long.TryParse(s, out w);
                    if (w >= 0)
                    {
                        iFrameInfo.Add(w);
                    }
                }

                if (!iFrameInfo.Contains(0))
                {
                    iFrameInfo.Insert(0, 0);
                }
                if (!iFrameInfo.Contains(numFrames))
                {
                    iFrameInfo.Add(numFrames);
                }
            }
        }

        public override void waitForFinish()
        {
            retrieved.WaitOne();
        }

        protected override void onExited(int exitCode)
        {
            if (exitCode != 0)
            {
                if (lastStderrLine == "")
                    lastStderrLine = "exitcode is " + exitCode.ToString();
                retrieved.Set();
            }
        }

        public IFrameInfo IFrameInfo
        {
            get {
                retrieved.WaitOne();
                if (!infoOk)
                    throw new Exception("iframe info failed: " + lastStderrLine);
                return iFrameInfo;
            }
        }
    }
}
