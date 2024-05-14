using System.Collections.Generic;

namespace OKEGui
{
    public class VideoInfoJob : Job
    {
        public List<string> VspipeArgs = new List<string>();
        public bool IsReEncode = false;

        public bool Vfr;
        public long FpsNum;
        public long FpsDen;

        public VideoInfoJob() : base()
        {
        }

        public override JobType GetJobType()
        {
            return JobType.VideoInfo;
        }
    }
}
