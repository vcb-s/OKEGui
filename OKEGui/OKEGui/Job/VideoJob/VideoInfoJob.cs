using System.Collections.Generic;

namespace OKEGui
{
    public class VideoInfoJob : Job
    {
        public List<string> VspipeArgs = new List<string>();
        public bool IsReEncode = false;
        public string WorkingPath;
        public string ReEncodeOldFile;

        public bool Vfr;
        public long FpsNum;
        public long FpsDen;
        public long NumberOfFrames;

        public VideoInfoJob() : base()
        {
        }

        public override JobType GetJobType()
        {
            return JobType.VideoInfo;
        }
    }
}
