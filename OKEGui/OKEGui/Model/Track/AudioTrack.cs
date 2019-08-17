using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class AudioTrack : Track
    {
        public AudioTrack(OKEFile file, AudioInfo info) : base(file, info)
        {
            TrackType = TrackType.Audio;
        }
    }
}
