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
        public bool IncludeSub;
        public string SubtitleLanguage;

        public Object Clone()
        {
            JobProfile clone = this.MemberwiseClone() as JobProfile;
            clone.AudioTracks = new List<AudioInfo>();
            foreach (AudioInfo info in this.AudioTracks)
            {
                clone.AudioTracks.Add(info.Clone() as AudioInfo);
            }
            return clone;
        }
    }
}
