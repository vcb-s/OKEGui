using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OKEGui.Utils
{
    public static class Constants
    {
        //QAAC encoder.
        public const string QAACPath = ".\\tools\\qaac\\qaac64.exe";
        public const int QAACBitrate = 192;

        //ffmpeg
        public const string ffmpegPath = ".\\tools\\ffmpeg\\ffmpeg.exe";

        //Audio & sub language.
        public const string language = "jpn";

        //Error messages and summaries
        public const string eac3toMissingMsg = "eac3to.exe 不存在。";
        public const string eac3toMissingSmr = "无EAC3TO";

        public const string audioNumMismatchMsg = "当前的视频含有轨道数{0}，与json中指定的数量{1}不符合。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string audioNumMismatchSmr = "音轨数不一致";

        public const string subNumMismatchMsg = "当前的视频含有字幕数{0}，与json中指定的数量{1}不符合。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string subNumMismatchSmr = "字幕数不一致";

        public const string fpsMismatchMsg = "输出FPS和指定FPS不一致。json里指定帧率为{0}，vs输出帧率为{1}。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string fpsMismatchSmr = "FPS不一致";

        public const string x264ErrorMsg = "x264出错:{0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string x264ErrorSmr = "x264出错";

        public const string x265ErrorMsg = "x265出错:{0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string x265ErrorSmr = "x265出错";

        public const string vpyErrorMsg = "vpy出错:{0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string vpyErrorSmr = "vpy出错";

        public const string unknownErrorMsg = "未知错误。该文件{0}将跳过处理。请转告技术总监复查。";
        public const string unknownErrorSmr = "未知错误";

        public const string vsCrashMsg = "压制未能完成，预计是vs崩溃。该文件{0}将跳过处理，半成品以HEVC形式保留在目录中。请转告技术总监复查。";
        public const string vsCrashSmr = "vs崩溃";

        public const string x264CrashMsg = "压制未能完成，预计是x264崩溃。该文件{0}将跳过处理，如果是MKV输出，半成品以_.mkv形式保留在目录中。请转告技术总监复查。";
        public const string x264CrashSmr = "x264崩溃";

        public const string x265CrashMsg = "压制未能完成，预计是x265崩溃。该文件{0}将跳过处理，半成品以HEVC形式保留在目录中。请转告技术总监复查。";
        public const string x265CrashSmr = "x265崩溃";

        public const string qaacErrorMsg = "QAAC无法正常运行。请确保你安装了Apple Application Support 64bit";
        public const string qaacErrorSmr = "QAAC无法运行";

        public const string audioFormatMistachMsg = "无法将{0}格式的音轨转为{1}格式。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string audioFormatMistachSmr = "音轨格式不匹配";

        public const string rpcErrorMsg = "RPC出错:{0}。请手动检查{1}，并请转告技术总监复查。";
        public const string rpcErrorSmr = "RPC出错";

        //Application configuration file
        public const string configFile = "OKEGuiConfig.json";

        //Input and Debug, Memory regex
        public static readonly Regex inputRegex = new Regex("# *OKE:INPUTFILE([\\s]+\\w+[ ]*=[ ]*)([r]*[\"'].*[\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static readonly Regex memoryRegex = new Regex("# *OKE:MEMORY([\\s]+core.max_cache_size+[ ]*=[ ]*)(\\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static readonly Regex debugRegex = new Regex("# *OKE:DEBUG([\\s]+\\w+[ ]*=[ ]*)(\\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        //Deprecated option list
        public static readonly List<string> deprecatedOptions = new List<string> { "SkipMuxing", "IncludeSub", "SubtitleLanguage" };

    }
}
