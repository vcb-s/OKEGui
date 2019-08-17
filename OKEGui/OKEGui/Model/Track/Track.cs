using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public enum TrackType
    {
        Default,
        Audio,
        Subtitle,
        Video,
        Chapter,
    }

    public class Track : ICloneable
    {
        public OKEFile File;
        public Info Info;
        public TrackType TrackType { get; protected set; } = TrackType.Default;

        public Track(OKEFile file, Info info)
        {
            File = file;
            Info = info;
        }

        public virtual Object Clone()
        {
            Track clone = this.MemberwiseClone() as Track;
            HandleCloned(clone);
            return clone;
        }


        protected virtual void HandleCloned(Track clone)
        {
            if (Info != null)
            {
                Info = Info.Clone() as Info;
            }
        }
    }
}
