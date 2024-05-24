using System;
using System.IO;
using OKEGui.Utils;
using System.Diagnostics;

namespace OKEGui.JobProcessor
{
    public delegate void EncoderOutputCallback(string line, int type);

    public abstract class CommandlineVideoEncoder : CommandlineJobProcessor
    {
        #region variables

        private long currentFrameNumber;

        protected double speed;
        protected double bitrate;
        protected string unit;

        protected VideoJob VJob
        {
            get { return job as VideoJob; }
        }

        #endregion variables

        public CommandlineVideoEncoder(VideoJob vjob) : base(vjob)
        {
        }

        protected bool SetFrameNumber(string frameString, bool isUpdateSpeed = false)
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

        protected bool SetSpeed(string speed, string unit = "fps")
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

        protected bool SetBitrate(string bitrate, string unit)
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
                VJob.TimeRemain = TimeSpan.FromDays(30);
            }
            else
            {
                VJob.TimeRemain = TimeSpan.FromSeconds((double)(VJob.NumberOfFrames - currentFrameNumber) / speed);
            }

            VJob.Speed = speed.ToString("0.00") + " fps";
            VJob.Progress = (double)currentFrameNumber / (double)VJob.NumberOfFrames * 100;

            if (bitrate == 0)
            {
                VJob.BitRate = "未知";
            }
            else
            {
                VJob.BitRate = bitrate.ToString("0.00") + " " + unit;
            }
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

        protected void EncodeFinish(long reportedFrames)
        {
            if (reportedFrames < VJob.NumberOfFrames)
            {
                OKETaskException ex = new OKETaskException(Constants.vsCrashSmr);
                throw ex;
            }
            VJob.TimeRemain = TimeSpan.Zero;
            VJob.Progress = 100;
            VJob.Status = "压制完成";

            // TODO: 计算最终码率
            // 这里显示文件最终大小
            FileInfo vinfo = new FileInfo(VJob.Output);
            VJob.BitRate = HumanReadableFilesize(vinfo.Length, 2);

            base.SetFinish();
        }

        public void AppendParameter(string param)
        {
            int pos = commandLine.Length - 2;
            commandLine = commandLine.Insert(pos, param + " ");
        }
    }
}
