namespace OKEGui
{
    class AudioJob : Job
    {
        public string CutFile;
        public int Delay;
        public string Language;
        public string Name;
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
                return "";
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
