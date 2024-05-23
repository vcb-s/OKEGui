using System;
using System.Collections.Generic;
using OKEGui.JobProcessor;
using OKEGui.Utils;

namespace OKEGui.Model
{
    public class IFrameInfo : List<long>
    {
        public IFrameInfo() : base()
        {
        }

        public IFrameInfo(IEnumerable<long> collection) : base(collection)
        {
        }

        public long FindNearestLeft(long begin)
        {
            return this.FindLast(x => x <= begin);
        }

        public long FindNearestRight(long end)
        {
            return this.Find(x => x >= end);
        }

        public SliceInfo FindInRangeIndex(SliceInfo range)
        {
            long begin = 0;
            bool flag = false;
            for (int i = 0; i < this.Count; i++)
            {
                if (range.begin <= this[i] && this[i] < range.end)
                {
                    begin = i;
                    flag = true;
                    break;
                }
            }
            if (!flag)
                return null;

            long end = begin;
            for (int i = (int)(begin + 1); i < this.Count; i++)
            {
                if (range.begin <= this[i] && this[i] < range.end)
                    end = i;
                else
                    break;
            }
            return new SliceInfo(begin, end);
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
            processor.CheckFps();

            if (j.IsReEncode)
            {
                IFrameInfoGenerator iframe = new IFrameInfoGenerator(j);
                iframe.start();
                iFrameInfo = iframe.IFrameInfo;
            }
        }
    }
}
