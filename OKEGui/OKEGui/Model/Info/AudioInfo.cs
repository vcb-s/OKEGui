using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class AudioInfo : Info
    {
        public string OutputCodec;
        public int Bitrate;
        [ObsoleteAttribute("SkipMuxing is obsolete. Use MuxOption instead.", false)]
        public bool SkipMuxing;

        public AudioInfo() : base()
        {
            InfoType = InfoType.Audio;
        }
    }
}
