using OKEGui.Model;

namespace OKEGui
{
    class AudioJob : Job
    {
        public readonly AudioInfo Info;

        public AudioJob(AudioInfo info) : base(info.OutputCodec)
        {
            Info = info;
        }

        public override JobType GetJobType()
        {
            return JobType.Audio;
        }
    }
}
