using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class JobProfile : ICloneable
    {
        public int Version;
        public string ProjectName;
        public string EncoderType;
        public string Encoder;
        public string EncoderParam;
        public string ContainerFormat;
        public string VideoFormat;
        public string AudioFormat;
        public double Fps;
        public uint FpsNum;
        public uint FpsDen;
        public List<AudioInfo> AudioTracks;
        public string InputScript;
        [ObsoleteAttribute("IncludeSub is obsolete. Use SubtitleTracks instead.", false)]
        public bool IncludeSub;
        [ObsoleteAttribute("SubtitleLanguage is obsolete. Use SubtitleTracks instead.", false)]
        public string SubtitleLanguage;
        public List<Info> SubtitleTracks;

        public Object Clone()
        {
            JobProfile clone = MemberwiseClone() as JobProfile;
            if (AudioTracks != null)
            {
                clone.AudioTracks = new List<AudioInfo>();
                foreach (AudioInfo info in AudioTracks)
                {
                    clone.AudioTracks.Add(info.Clone() as AudioInfo);
                }
            }
            if (SubtitleTracks != null)
            {
                clone.SubtitleTracks = new List<Info>();
                foreach(Info info in SubtitleTracks)
                {
                    clone.SubtitleTracks.Add(info.Clone() as Info);
                }
            }
            return clone;
        }
    }
}
