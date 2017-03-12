namespace OKEGui
{
    public enum MuxerType { MP4BOX, MKVMERGE, AVIMUXGUI, TSMUXER, FFMPEG };

    public class MuxJob : Job
    {
        private string codec;
        private int nbOfBframes, bitrate;
        private ulong nbOfFrames;
        private string fps;

        private double overhead;

        //private MuxSettings settings;
        private MuxerType type;

        public override string CodecString
        {
            get { return type.ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture); }
        }

        public override string JobType
        {
            get { return "mux"; }
        }

        public MuxJob() : base()
        {
            codec = "";
            nbOfBframes = 0;
            bitrate = 0;
            overhead = 4.3;
            //type = MuxerType.MP4BOX;
            //containerType = ContainerType.MP4;
            //settings = new MuxSettings();
        }

        /// <summary>
        /// codec used for video, used for informational purposes (put in the log)
        /// </summary>
        public string Codec
        {
            get { return codec; }
            set { codec = value; }
        }

        /// <summary>
        /// number of b-frames in the video input, used for informational purposes
        /// </summary>
        public int NbOfBFrames
        {
            get { return nbOfBframes; }
            set { nbOfBframes = value; }
        }

        /// <summary>
        /// the number of frames the video has
        /// </summary>
        public ulong NbOfFrames
        {
            get { return nbOfFrames; }
            set { nbOfFrames = value; }
        }

        /// <summary>
        /// chosen video bitrate for the output, used for informational purposes
        /// </summary>
        public int Bitrate
        {
            get { return bitrate; }
            set { bitrate = value; }
        }

        /// <summary>
        /// projected mp4 muxing overhead for this job in bytes / frame
        /// </summary>
        public double Overhead
        {
            get { return overhead; }
            set { overhead = value; }
        }

        ///// <summary>
        ///// the settings for this job
        ///// contains additional information like additional streams, framerate, etc.
        ///// </summary>
        //public MuxSettings Settings
        //{
        //    get { return settings; }
        //    set { settings = value; }
        //}

        ///// <summary>
        ///// gets / sets the type of mux job this is
        ///// </summary>
        //public MuxerType MuxType
        //{
        //    get { return type; }
        //    set { type = value; }
        //}
    }
}
