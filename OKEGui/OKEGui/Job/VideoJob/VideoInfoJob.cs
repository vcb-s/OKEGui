using System.Collections.Generic;

namespace OKEGui
{
    public class VideoInfoJob : Job
    {
        public List<string> Args = new List<string>();
        public VideoJob vJob;
        public VideoInfoJob(VideoJob job) : base()
        {
            vJob = job;
            Input = job.Input;
            Args.AddRange(job.VspipeArgs);
        }

        public override JobType GetJobType()
        {
            return JobType.VideoInfo;
        }
    }
}
