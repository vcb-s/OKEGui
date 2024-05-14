using System.Collections.Generic;
using OKEGui.Model;

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
        public long FrameBegin;
        public long FrameEnd;
        public bool IsPartialEncode;

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
