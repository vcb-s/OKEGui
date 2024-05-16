using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using OKEGui.JobProcessor;
using OKEGui.Utils;

namespace OKEGui
{
    public class VSPipeInfoProcessor : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("VSPipeInfoProcessor");
        private VSVideoInfo videoInfo;
        private bool videoInfoOk;
        private string lastStderrLine;
        private ManualResetEvent retrieved = new ManualResetEvent(false);
        private VideoInfoJob job;
        private bool isVSError = false;
        private string errorMsg;

        public VSPipeInfoProcessor(VideoInfoJob j) : base()
        {
            // 获取VSPipe路径
            executable = Initializer.Config.vspipePath;
            videoInfo = new VSVideoInfo();
            videoInfoOk = false;
            job = j;

            StringBuilder sb = new StringBuilder();

            sb.Append("--info");
            foreach (string arg in j.VspipeArgs)
            {
                sb.Append($" --arg \"{arg}\"");
            }
            sb.Append(" \"" + j.Input + "\"");
            sb.Append(" -");

            commandLine = sb.ToString();
        }

        public void CheckFps(VideoInfoJob j)
        {
            j.NumberOfFrames = videoInfo.numFrames;

            if (j.Vfr)
            {
                j.FpsNum = videoInfo.fpsNum;
                j.FpsDen = videoInfo.fpsDen;
                videoInfo.vfr = true;
                videoInfo.fps = (double)videoInfo.fpsNum / videoInfo.fpsDen;
                return;
            }
            else if (videoInfo.fpsNum == j.FpsNum && videoInfo.fpsDen == j.FpsDen)
            {
                videoInfo.vfr = false;
                videoInfo.fps = (double)videoInfo.fpsNum / videoInfo.fpsDen;
                return;
            }
            else
            {
                OKETaskException ex = new OKETaskException(Constants.fpsMismatchSmr);
                ex.progress = 0.0;
                ex.Data["SRC_FPS"] = ((double)j.FpsNum / j.FpsDen).ToString("F3");
                ex.Data["DST_FPS"] = ((double)videoInfo.fpsNum / videoInfo.fpsDen).ToString("F3");
                throw ex;
            }
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Logger.Debug(line);
            if (stream == StreamType.Stderr)
                lastStderrLine = line;
            Regex rWidth = new Regex("Width: ([0-9]+)");
            Regex rHeight = new Regex("Height: ([0-9]+)");
            Regex rFrames = new Regex("Frames: ([0-9]+)");
            Regex rFPS = new Regex("FPS: ([0-9]+)/([0-9]+) \\(([0-9]+.[0-9]+) fps\\)");
            Regex rFormatName = new Regex("Format Name: ([a-zA-Z0-9]+)");
            Regex rColorFamily = new Regex("Color Family: ([a-zA-Z]+)");
            Regex rBits = new Regex("Bits: ([0-9]+)");
            Regex rlwiProgress = new Regex("Creating lwi index file ([0-9]+)%");

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
            else if (line.Contains("Width"))
            {
                var s = rWidth.Split(line);
                int w;
                int.TryParse(s[1], out w);
                if (w > 0)
                {
                    videoInfo.width = w;
                }
            }
            else if (line.Contains("Height"))
            {
                var s = rHeight.Split(line);
                int h;
                int.TryParse(s[1], out h);
                if (h > 0)
                {
                    videoInfo.height = h;
                }
            }
            else if (line.Contains("Frames"))
            {
                var s = rFrames.Split(line);
                long f;
                long.TryParse(s[1], out f);
                if (f > 0)
                {
                    videoInfo.numFrames = f;
                }
            }
            else if (line.Contains("FPS"))
            {
                if (line.Contains("Variable"))
                {
                    throw new Exception("VFR output not supported, even for VFR jobs.");
                }
                var s = rFPS.Split(line);

                long n;
                long.TryParse(s[1], out n);
                if (n > 0)
                {
                    videoInfo.fpsNum = n;
                }

                long.TryParse(s[2], out n);
                if (n > 0)
                {
                    videoInfo.fpsDen = n;
                }

                double f;
                double.TryParse(s[3], out f);
                if (f > 0)
                {
                    videoInfo.fps = f;
                }
            }
            else if (line.Contains("Format Name:"))
            {
                var s = rFormatName.Split(line);
                videoInfo.format.name = s[1];
            }
            else if (line.Contains("Color Family"))
            {
                var s = rColorFamily.Split(line);
                videoInfo.format.colorFamilyName = s[1];
            }
            else if (line.Contains("Bits"))
            {
                var s = rBits.Split(line);
                int w;
                int.TryParse(s[1], out w);
                if (w > 0)
                {
                    videoInfo.format.bitsPerSample = w;
                }

                // 假设到这里已经获取完毕了
                videoInfoOk = true;
                retrieved.Set();
            }
            else if (line.Contains("SubSampling"))
            {
                //目前还没有要处理subsampling的
            }
            else if (rlwiProgress.IsMatch(line))
            {
                var s = rlwiProgress.Split(line);
                int p;
                int.TryParse(s[1], out p);
                job.Progress = p;
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

        public VSVideoInfo VideoInfo
        {
            get {
                retrieved.WaitOne();
                if (!videoInfoOk)
                    throw new Exception("vspipe -i failed: " + lastStderrLine);
                return videoInfo;
            }
        }
    }
}
