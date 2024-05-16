using System;
using System.Collections.Generic;

namespace OKEGui.Model
{
    public class IFrameInfo : List<long>
    {
        public long FindNearestLeft(long begin)
        {
            return this.FindLast(x => x <= begin);
        }

        public long FindNearestRight(long end)
        {
            return this.Find(x => x >= end);
        }

        public override string ToString()
        {
            string str = "[ ";
            foreach (var s in this)
            {
                str += $"{s}, ";
            }
            str += "]";
            return str;
        }
    }

    public class VSPipeInfo
    {
        public VSVideoInfo videoInfo;

        public IFrameInfo iFrameInfo;

        public VSPipeInfo(VideoInfoJob j)
        {
            VSPipeInfoProcessor processor = new VSPipeInfoProcessor(j);
            processor.start();
            videoInfo = processor.VideoInfo;
            processor.CheckFps(j);

            if (j.IsReEncode)
            {
                IFrameInfoGenerator iframe = new IFrameInfoGenerator(j);
                iframe.start();
                iFrameInfo = iframe.IFrameInfo;
            }
        }
    }
}
