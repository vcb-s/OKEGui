using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class SubtitleTrack : Track
    {
        public SubtitleTrack(OKEFile file, Info info) : base(file, info)
        {
            if (info.InfoType != InfoType.Default)
            {
                throw new ArgumentException("Invalid media info for subtitle track");
            }
            TrackType = TrackType.Subtitle;
        }
    }
}
