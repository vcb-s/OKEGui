using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class VideoSliceTrack : VideoTrack
    {
        public VideoSliceTrack(OKEFile file, VideoSliceInfo info) : base(file, info)
        {
            TrackType = TrackType.Video;
        }
    }
}
