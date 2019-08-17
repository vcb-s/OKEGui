using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class VideoInfo : Info
    {
        public uint FpsNum;
        public uint FpsDen = 1;

        public VideoInfo() : base()
        {
            InfoType = InfoType.Video;
        }

        public VideoInfo(uint fpsNum, uint fpsDen) : this()
        {
            FpsNum = fpsNum;
            FpsDen = fpsDen;
        }

        public double GetFps()
        {
            return (double)FpsNum / FpsDen;
        }
    }
}
