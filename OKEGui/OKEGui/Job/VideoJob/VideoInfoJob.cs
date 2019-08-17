namespace OKEGui
{
    public class VideoInfoJob : Job
    {
        public VideoInfoJob(string input) : base()
        {
            this.Input = input;
        }

        public override JobType GetJobType()
        {
            return JobType.VideoInfo;
        }
    }
}
