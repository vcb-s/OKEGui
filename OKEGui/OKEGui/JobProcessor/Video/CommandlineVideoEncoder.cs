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

        public long NumberOfFrames { get; protected set; }
        private long currentFrameNumber;

        protected double speed;
        protected double bitrate;
        protected string unit;

        protected VideoJob job;

        #endregion variables

        protected bool setFrameNumber(string frameString, bool isUpdateSpeed = false)
        {
            long currentFrame;
            if (long.TryParse(frameString, out currentFrame))
            {
                if (currentFrame < 0)
                {
                    currentFrameNumber = 0;
                    return false;
                }
                else
                {
                    currentFrameNumber = currentFrame;
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

        protected void encodeFinish(long reportedFrames)
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
