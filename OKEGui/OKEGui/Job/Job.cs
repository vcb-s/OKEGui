using System;
using System.Collections.Generic;

namespace OKEGui
{
    public enum ProcessPriority : int { IDLE = 0, BELOW_NORMAL, NORMAL, ABOVE_NORMAL, HIGH, PARALLEL };

    public enum JobType
    {
        Video,
        Audio,
        VideoInfo,
        Mux,
        RpCheck
    }

    /// <summary>
    /// 任务信息
    /// </summary>
    public abstract class Job
    {
        #region important details

        public string Input;
        public string Output;
        public List<string> FilesToDelete;

        #endregion important details

        #region JobStatus

        public string Status
        {
            set
            {
                if (ts != null)
                {
                    ts.CurrentStatus = value;
                }
            }
        }

        public double Progress
        {
            set
            {
                if (ts != null)
                {
                    ts.ProgressValue = value;
                }
            }
        }

        public string Speed
        {
            set
            {
                if (ts != null)
                {
                    ts.Speed = value;
                }
            }
        }

        public TimeSpan TimeRemain
        {
            set
            {
                if (ts != null)
                {
                    ts.TimeRemain = value;
                }
            }
        }

        public string BitRate
        {
            set
            {
                if (ts != null)
                {
                    ts.BitRate = value;
                }
            }
        }

        protected TaskStatus ts;

        public void SetUpdate(TaskStatus taskStatus)
        {
            ts = taskStatus;
        }

        #endregion JobStatus

        #region init
        public Job()
        {

        }

        public Job(string codec) : base()
        {
            CodecString = codec.ToUpper();
        }

        #endregion init

        #region queue display details

        /// <summary>
        /// 使用的编码格式
        /// </summary>
        public string CodecString;

        public abstract JobType GetJobType();

        #endregion queue display details
    }
}
