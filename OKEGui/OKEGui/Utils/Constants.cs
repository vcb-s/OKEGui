using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Utils
{
    public static class Constants
    {
        //Bitrate for QAAC encoder.
        public const int QAACBitrate = 192;

        //Audio & sub language.
        public const string language = "jpn";

        //Error messages and summaries
        public const string eac3toMissingMsg = "eac3to.exe 不存在。";
        public const string eac3toMissingSmr = "无EAC3TO";

        public const string audioNumMismatchMsg = "当前的视频含有轨道数{0}，与json中指定的数量{1}不符合。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string audioNumMismatchSmr = "音轨数不一致";

        public const string fpsMismatchMsg = "输出FPS和指定FPS不一致。json里指定帧率为{0}，vs输出帧率为{1}。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string fpsMismatchSmr = "FPS不一致";

        public const string x265ErrorMsg = "x265出错:{0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string x265ErrorSmr = "x265出错";

        public const string vpyErrorMsg = "vpy出错:{0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string vpyErrorSmr = "vpy出错";

        public const string unknownErrorMsg = "未知错误。该文件{0}将跳过处理。请转告技术总监复查。";
        public const string unknownErrorSmr = "未知错误";
    }
}
