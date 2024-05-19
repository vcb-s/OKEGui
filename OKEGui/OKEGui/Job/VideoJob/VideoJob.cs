using System.Collections.Generic;
using OKEGui.Model;
using OKEGui.Utils;

namespace OKEGui
{
    public class VideoJob : Job
    {
        public readonly VideoInfo Info;
        public string EncoderPath;
        public string EncodeParam;
        public List<string> VspipeArgs = new List<string>();
        public int NumaNode;
        public long NumberOfFrames;
        public SliceInfo FrameRange;
        public bool IsPartialEncode;
        public int PartId;

        public VideoJob(VideoInfo info, string codec) : base(codec)
        {
            Info = info;
        }

        public override JobType GetJobType()
        {
            return JobType.Video;
        }
    }
}
