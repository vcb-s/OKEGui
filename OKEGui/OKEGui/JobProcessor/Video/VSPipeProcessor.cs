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
    public class VSPipeProcessor : CommandlineJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private VSVideoInfo videoInfo;
        private bool videoInfoOk;
        private string lastStderrLine;
        private ManualResetEvent retrieved = new ManualResetEvent(false);

        public VSPipeProcessor(VideoInfoJob j) : base()
        {
            // 获取VSPipe路径
            executable = Initializer.Config.vspipePath;
            videoInfo = new VSVideoInfo();
            videoInfoOk = false;

            StringBuilder sb = new StringBuilder();

            sb.Append("--info");
            foreach (string arg in j.Args)
            {
                sb.Append($" --arg \"{arg}\"");
            }
            sb.Append(" \"" + j.Input + "\"");
            sb.Append(" -");

            commandLine = sb.ToString();
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

            if (line.Contains("Python exception: "))
            {
                OKETaskException ex = new OKETaskException(Constants.vpyErrorSmr);
                ex.progress = 0.0;
                ex.Data["VPY_ERROR"] = line.Substring(18);
                throw ex;
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
                int f;
                int.TryParse(s[1], out f);
                if (f > 0)
                {
                    videoInfo.numFrames = f;
                }
            }
            else if (line.Contains("FPS"))
            {
                var s = rFPS.Split(line);

                int n;
                int.TryParse(s[1], out n);
                if (n > 0)
                {
                    videoInfo.fpsNum = n;
                }

                int.TryParse(s[2], out n);
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
