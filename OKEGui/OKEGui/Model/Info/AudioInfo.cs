using OKEGui.Utils;

namespace OKEGui.Model
{
    public class AudioInfo : Info
    {
        public string OutputCodec;
        public int Bitrate = Constants.QAACBitrate;
        public int? Quality;
        public bool Lossy = false;
        public int Length;

        public AudioInfo() : base()
        {
            InfoType = InfoType.Audio;
        }
    }
}
