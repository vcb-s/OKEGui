using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class ChapterTrack : Track
    {
        public ChapterTrack(OKEFile file) : base(file, new Info())
        {
            TrackType = TrackType.Chapter;
        }
    }
}
