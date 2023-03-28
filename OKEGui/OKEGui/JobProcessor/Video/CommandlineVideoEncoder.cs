using System;
using System.IO;
using OKEGui.Utils;
using OKEGui.JobProcessor;
using System.Diagnostics;

namespace OKEGui
{
    public delegate void EncoderOutputCallback(string line, int type);

    public abstract class CommandlineVideoEncoder : CommandlineJobProcessor
    {
        #region variables

        public ulong NumberOfFrames { get; protected set; }
        private ulong currentFrameNumber;
        protected long fps_n = 0, fps_d = 0;

        protected bool usesSAR = false;
        protected double speed;
        protected double bitrate;
        protected string unit;

        protected VideoJob job;

        #endregion variables

        #region helper methods

        /// <summary>
        /// tries to open the video source and gets the number of frames from it, or
        /// exits with an error
        /// </summary>
        /// <param name="videoSource">the AviSynth script</param>
        /// <param name="error">return parameter for all errors</param>
        /// <returns>true if the file could be opened, false if not</returns>
        protected void getInputProperties(VideoJob job)
        {
            //VapourSynthHelper vsHelper = new VapourSynthHelper();
            //vsHelper.LoadScriptFile(job.Input);
            VSPipeInfo vsHelper = new VSPipeInfo(job);
            fps_n = vsHelper.FpsNum;
            fps_d = vsHelper.FpsDen;
            NumberOfFrames = (ulong)vsHelper.TotalFreams;
            job.NumberOfFrames = NumberOfFrames;
            if (fps_n == job.FpsNum && fps_d == job.FpsDen) return;
            if (job.Vfr)
            {
                job.FpsNum = (uint)fps_n;
                job.FpsDen = (uint)fps_d;
                job.Fps = (fps_n + 0.0) / fps_d;
                return;
            }
            OKETaskException ex = new OKETaskException(Constants.fpsMismatchSmr);
            ex.progress = 0.0;
            ex.Data["SRC_FPS"] = ((double)job.FpsNum / job.FpsDen).ToString("F3");
            ex.Data["DST_FPS"] = ((double)fps_n / fps_d).ToString("F3");
            throw ex;
        }

        #endregion helper methods

        protected bool setFrameNumber(string frameString, bool isUpdateSpeed = false)
        {
            int currentFrame;
            if (int.TryParse(frameString, out currentFrame))
            {
                if (currentFrame < 0)
                {
                    currentFrameNumber = 0;
                    return false;
                }
                else
                {
                    currentFrameNumber = (ulong)currentFrame;
                }

                Update();
                return true;
            }
            return false;
        }

        protected bool setSpeed(string speed, string unit = "fps")
        {
            double fps, factor = 1;
            if (unit == "fpm")
                factor = 60;
            if (double.TryParse(speed, out fps))
            {
                if (fps > 0)
                {
                    this.speed = fps / factor;
                }
                else
                {
                    this.speed = 0;
                }

                Update();
                return true;
            }

            return false;
        }

        protected bool setBitrate(string bitrate, string unit)
        {
            double rate;
            this.unit = unit;
            if (double.TryParse(bitrate, out rate))
            {
                if (rate > 0)
                {
                    this.bitrate = rate;
                }
                else
                {
                    this.bitrate = 0;
                }

                Update();
                return true;
            }

            return false;
        }

        protected void Update()
        {
            if (speed == 0)
            {
                job.TimeRemain = TimeSpan.FromDays(30);
            }
            else
            {
                job.TimeRemain = TimeSpan.FromSeconds((double)(NumberOfFrames - currentFrameNumber) / speed);
            }

            job.Speed = speed.ToString("0.00") + " fps";
            job.Progress = (double)currentFrameNumber / (double)NumberOfFrames * 100;

            if (bitrate == 0)
            {
                job.BitRate = "未知";
            }
            else
            {
                job.BitRate = bitrate.ToString("0.00") + " " + unit;
            }

            // su.NbFramesDone = currentFrameNumber;
        }

        public static String HumanReadableFilesize(double size, int digit)
        {
            String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            int i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }

            return Math.Round(size * Math.Pow(10, digit)) / Math.Pow(10, digit) + " " + units[i];
        }

        protected void encodeFinish(ulong reportedFrames)
        {
            if (reportedFrames < NumberOfFrames)
            {
                OKETaskException ex = new OKETaskException(Constants.vsCrashSmr);
                throw ex;
            }
            job.TimeRemain = TimeSpan.Zero;
            job.Progress = 100;
            job.Status = "压制完成";

            // TODO: 计算最终码率
            // 这里显示文件最终大小
            FileInfo vinfo = new FileInfo(job.Output);
            job.BitRate = HumanReadableFilesize(vinfo.Length, 2);

            base.SetFinish();
        }

        public void AppendParameter(string param)
        {
            int pos = commandLine.Length - 2;
            commandLine = commandLine.Insert(pos, param + " ");
        }
    }
}
