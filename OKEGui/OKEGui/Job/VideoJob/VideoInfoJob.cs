using System.Collections.Generic;

namespace OKEGui
{
    public class VideoInfoJob : Job
    {
        public List<string> Args = new List<string>();
        public VideoInfoJob(string input, List<string> args) : base()
        {
            Input = input;
            Args.AddRange(args);
        }

        public override JobType GetJobType()
        {
            return JobType.VideoInfo;
        }
    }
}
