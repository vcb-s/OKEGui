using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class VideoInfo : Info
    {
        public long FpsNum;
        public long FpsDen = 1;
        public IFrameInfo ChapterIFrameInfo;
        public string TimeCodeFile;
        public string QpFile;

        public VideoInfo() : base()
        {
            InfoType = InfoType.Video;
        }
        public VideoInfo(long fpsNum, long fpsDen, string timeCodeFile, string qpFile, IFrameInfo chapterIFrameInfo) : this()
        {
            ChapterIFrameInfo = chapterIFrameInfo;
            TimeCodeFile = timeCodeFile;
            QpFile = qpFile;
            FpsNum = fpsNum;
            FpsDen = fpsDen;
        }

        public double GetFps()
        {
            return (double)FpsNum / FpsDen;
        }
    }
}
