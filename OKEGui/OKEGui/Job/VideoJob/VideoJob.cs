namespace OKEGui
{
    public class Zone
    {
        public int startFrame;
        public int endFrame;
    }

    /// <summary>
    /// Summary description for VideoJob.
    /// </summary>
    public class VideoJob : Job
    {
        private string codecString;
        public string EncoderPath;
        public string EncodeParam;
        public double Fps;

        public VideoJob() : base()
        {
        }

        public VideoJob(string codec) : base()
        {
            codecString = codec.ToUpper();
        }

        private Zone[] zones = new Zone[] { };

        /// <summary>
        /// gets / sets the zones
        /// </summary>
        public Zone[] Zones
        {
            get { return zones; }
            set { zones = value; }
        }

        /// <summary>
        /// codec used as presentable string
        /// </summary>
        public override string CodecString
        {
            get {
                return codecString;
            }
        }

        /// <summary>
        /// returns the encoding mode as a human readable string
        /// (this string is placed in the appropriate column in the queue)
        /// </summary>
        public override string JobType
        {
            get {
                return "video";
            }
        }
    }
}
