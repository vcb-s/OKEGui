using System;
using System.Collections.Generic;

/// <summary>
/// 记录一个Task所需要的所有信息。跟Json文件挂钩。
/// </summary>
namespace OKEGui.Model
{
    public class TaskProfile : ICloneable
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
        [ObsoleteAttribute("IncludeSub is obsolete. Use SubtitleTracks instead.", false)]
        public bool IncludeSub;
        [ObsoleteAttribute("SubtitleLanguage is obsolete. Use SubtitleTracks instead.", false)]
        public string SubtitleLanguage;
        public List<Info> SubtitleTracks;
        public List<string> InputFiles;

        public Object Clone()
        {
            TaskProfile clone = MemberwiseClone() as TaskProfile;
            if (AudioTracks != null)
            {
                clone.AudioTracks = new List<AudioInfo>();
                foreach (AudioInfo info in AudioTracks)
                {
                    clone.AudioTracks.Add(info.Clone() as AudioInfo);
                }
            }
            if (SubtitleTracks != null)
            {
                clone.SubtitleTracks = new List<Info>();
                foreach(Info info in SubtitleTracks)
                {
                    clone.SubtitleTracks.Add(info.Clone() as Info);
                }
            }
            return clone;
        }

        public override string ToString()
        {
            string str = "项目名字: " + ProjectName;
            str += "\n\n编码器类型: " + EncoderType;
            str += "\n编码器路径: " + Encoder;
            str += "\n编码参数: " + EncoderParam.Substring(0, Math.Min(30, EncoderParam.Length - 1)) + "......";
            str += "\n\n封装格式: " + ContainerFormat;
            str += "\n视频编码: " + VideoFormat;
            str += "\n视频帧率: " + string.Format("{0:0.000} fps", Fps);
            str += "\n音频编码(主音轨): " + AudioFormat;
            str += "\n输入文件数量: " + InputFiles?.Count;

            return str;
        }
    }
}
