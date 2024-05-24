using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class ChapterTrack : Track
    {
        public ChapterTrack(OKEFile file, string language) : base(file, new Info())
        {
            TrackType = TrackType.Chapter;
            Info.Language = language;
        }
    }
}
