using System;
using System.Collections.Generic;
using OKEGui.Utils;

namespace OKEGui.Model
{
    public class VideoSliceInfo : VideoInfo
    {
        public bool IsReEncode;
        public SliceInfo FrameRange;
        public int PartId;

        public VideoSliceInfo(VideoInfo info) : base()
        {
            FpsNum = info.FpsNum;
            FpsDen = info.FpsDen;
        }
        public VideoSliceInfo(bool isReEncode, SliceInfo frameRange, int partId, string qpFile, IFrameInfo chapterIFrameInfo, VideoInfo info) : this(info)
        {
            IsReEncode = isReEncode;
            FrameRange = frameRange;
            PartId = partId;
            QpFile = qpFile;
            ChapterIFrameInfo = chapterIFrameInfo;
        }
    }
}
