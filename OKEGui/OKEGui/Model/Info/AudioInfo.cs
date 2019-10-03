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

        public AudioInfo() : base()
        {
            InfoType = InfoType.Audio;
        }
    }
}
