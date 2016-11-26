using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OKEGui
{
    public class VSPipeProcessor : CommandlineJobProcessor
    {
        public static IJobProcessor NewVSPipeProcessor(Job j)
        {
            if (j is VideoInfoJob) {
                return new VSPipeProcessor(j as VideoInfoJob);
            }
            return null;
        }

        private string commandLine;
        private VSVideoInfo videoInfo;
        private ManualResetEvent retrieved = new ManualResetEvent(false);

        public VSPipeProcessor(VideoInfoJob j) : base()
        {
            // 获取VSPipe路径
            RegistryKey key = Registry.LocalMachine;
            RegistryKey vskey = key.OpenSubKey("software\\vapoursynth");
            string vscore = vskey.GetValue("Path") as string;
            if (vscore == null) {
                throw new Exception("can't get vs install path");
            }

            FileInfo vspipeInfo = new FileInfo(new DirectoryInfo(vscore).FullName + "\\core64\\vspipe.exe");

            if (vspipeInfo.Exists) {
                this.executable = vspipeInfo.FullName;
            }

            videoInfo = new VSVideoInfo();

            StringBuilder sb = new StringBuilder();

            sb.Append("--info ");
            sb.Append("\"" + j.Input + "\" ");
            sb.Append("-");

            commandLine = sb.ToString();
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            Regex rWidth = new Regex("Width: ([0-9]+)");
            Regex rHeight = new Regex("Height: ([0-9]+)");
            Regex rFrames = new Regex("Frames: ([0-9]+)");
            Regex rFPS = new Regex("FPS: ([0-9]+)/([0-9]+) \\(([0-9]+.[0-9]+) fps\\)");
            Regex rFormatName = new Regex("Format Name: ([a-zA-Z0-9]+)");
            Regex rColorFamily = new Regex("Color Family: ([a-zA-Z]+)");
            Regex rBits = new Regex("Bits: ([0-9]+)");

            if (line.Contains("Width")) {
                var s = rWidth.Split(line);
                int w;
                int.TryParse(s[1], out w);
                if (w > 0) {
                    videoInfo.width = w;
                }
            } else if (line.Contains("Height")) {
                var s = rHeight.Split(line);
                int h;
                int.TryParse(s[1], out h);
                if (h > 0) {
                    videoInfo.height = h;
                }
            } else if (line.Contains("Frames")) {
                var s = rFrames.Split(line);
                int f;
                int.TryParse(s[1], out f);
                if (f > 0) {
                    videoInfo.numFrames = f;
                }
            } else if (line.Contains("FPS")) {
                var s = rFPS.Split(line);

                int n;
                int.TryParse(s[1], out n);
                if (n > 0) {
                    videoInfo.fpsNum = n;
                }

                int.TryParse(s[2], out n);
                if (n > 0) {
                    videoInfo.fpsDen = n;
                }

                double f;
                double.TryParse(s[3], out f);
                if (f > 0) {
                    videoInfo.fps = f;
                }
            } else if (line.Contains("Format Name:")) {
                var s = rFormatName.Split(line);
                videoInfo.format.name = s[1];
            } else if (line.Contains("Color Family")) {
                var s = rColorFamily.Split(line);
                videoInfo.format.colorFamilyName = s[1];
            } else if (line.Contains("Bits")) {
                var s = rBits.Split(line);
                int w;
                int.TryParse(s[1], out w);
                if (w > 0) {
                    videoInfo.format.bitsPerSample = w;
                }

                // 假设到这里已经获取完毕了
                retrieved.Set();
            }
        }

        public void waitForFinish()
        {
            retrieved.WaitOne();
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

        public VSVideoInfo VideoInfo
        {
            get {
                retrieved.WaitOne();
                return videoInfo;
            }
        }
    }
}
