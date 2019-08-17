namespace OKEGui
{
    public class VideoJob : Job
    {
        public string EncoderPath;
        public string EncodeParam;
        public double Fps;
        public uint FpsNum;
        public uint FpsDen;
        public int NumaNode;

        public VideoJob(string codec) : base(codec)
        {
        }

        public override JobType GetJobType()
        {
            return JobType.Video;
        }
    }
}
