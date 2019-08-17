using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class VideoTrack : Track
    {
        public VideoTrack(OKEFile file, VideoInfo info) : base(file, info)
        {
            TrackType = TrackType.Video;
        }
    }
}
