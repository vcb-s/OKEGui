using OKEGui.Model;

namespace OKEGui
{
    class AudioJob : Job
    {
        public string Language;
        public int Bitrate;

        public AudioJob(string codec) : base(codec)
        {

        }

        public override JobType GetJobType()
        {
            return JobType.Audio;
        }
    }
}
