namespace OKEGui
{
    internal class AudioJob : Job
    {
        public string CutFile;
        public int Delay;
        public string Language;
        public string Name;
        public int Bitrate;
        private string codecString;

        public AudioJob() : base()
        {
        }

        public AudioJob(string codec) : base()
        {
            codecString = codec.ToUpper();
        }

        public override string CodecString
        {
            get {
                return codecString;
            }
        }

        public override string JobType
        {
            get {
                return "audio";
            }
        }
    }
}
