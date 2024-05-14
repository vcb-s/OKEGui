using System;
using System.Collections.Generic;

namespace OKEGui.Model
{
    public class IFrameInfo
    {
        public List<int> iFrameArray;
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
            processor.checkFps(j);
        }
    }
}
