namespace OKEGui
{
    public enum ZONEMODE : int { Quantizer = 0, Weight };

    public class Zone
    {
        public int startFrame;
        public int endFrame;
        public ZONEMODE mode;
        public decimal modifier;
    }

    /// <summary>
    /// Summary description for VideoJob.
    /// </summary>
    public class VideoJob : Job
    {
        private string codecString;
        public JobDetails config;

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

        private Dar? dar;

        public Dar? DAR
        {
            get { return dar; }
            set { dar = value; }
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
