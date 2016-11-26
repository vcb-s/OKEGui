namespace OKEGui
{
    public class VideoInfoJob : Job
    {
        public VideoInfoJob(string input) : base()
        {
            this.Input = input;
        }

        public override string JobType
        {
            get { return "video info"; }
        }

        public override string CodecString
        {
            get { return ""; }
        }
    }
}
