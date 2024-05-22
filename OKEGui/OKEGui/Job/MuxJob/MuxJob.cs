using System.Collections.Generic;
using OKEGui.Model;
using OKEGui.Utils;

namespace OKEGui
{
    public enum MuxType
    {
        SingleVideo,
        AppendVideo,
        MergeOldRemux
    }

    public class MuxJob : Job
    {
        public readonly MuxType MuxType;
        public readonly VideoInfo Info;
        public List<VideoSliceTrack> VideoSlices = new List<VideoSliceTrack>();
        public string TimeCodeFile;
        public string ReEncodeOldFile;
        public SliceInfo FrameRange;
        public bool IsPartialMux;
        public int PartId;

        public MuxJob(MuxType muxType, string containerFormat) : base(containerFormat)
        {
            MuxType = muxType;
        }

        public MuxJob(MuxType muxType, string containerFormat, VideoInfo info) : this(muxType, containerFormat)
        {
            Info = info;
        }

        public override JobType GetJobType()
        {
            return JobType.Mux;
        }
    }
}
