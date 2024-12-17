﻿using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OKEGui.Utils
{
    public static class Constants
    {
        //QAAC encoder.
        public const string QAACPath = ".\\tools\\qaac\\qaac64.exe";
        public const int QAACBitrate = 192;
        public const int QAACQualityMin = 0;
        public const int QAACQualityMax = 127;

        //ffmpeg
        public const string ffmpegPath = ".\\tools\\ffmpeg\\ffmpeg.exe";

        //eac3to-wrapper
        public const string eac3toWrapperPath = ".\\tools\\eac3to\\eac3to-wrapper.exe";

        //vspipe
        public const string vspipePath = ".\\tools\\vapoursynth\\vspipe.exe";

        //x264
        public const string x264Path = ".\\tools\\x26x\\x264.exe";

        //x265
        public const string x265Path = ".\\tools\\x26x\\x265.exe";

        //mkvmerge
        public const string mkvmergePath = ".\\tools\\mkvtoolnix\\mkvmerge.exe";

        //l-smash
        public const string lsmashPath = ".\\tools\\l-smash\\muxer.exe";

        //rpchecker
        public const string rpcPath = ".\\tools\\rpc\\rpchecker.exe";

        //Audio & sub language.
        public const string language = "jpn";

        //Error messages and summaries
       
        public const string audioNumMismatchMsg = "当前的视频含有轨道数{0}，与json中指定的数量{1}必须+{2}可选不符合。该文件{3}将跳过处理。请转告技术总监复查。";
        public const string audioNumMismatchSmr = "音轨数不一致";

        public const string subNumMismatchMsg = "当前的视频含有字幕数{0}，与json中指定的数量{1}必须+{2}可选不符合。该文件{3}将跳过处理。请转告技术总监复查。";
        public const string subNumMismatchSmr = "字幕数不一致";

        public const string fpsMismatchMsg = "输出FPS和指定FPS不一致。json里指定帧率为{0}，vs输出帧率为{1}。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string fpsMismatchSmr = "FPS不一致";

        public const string x264ErrorMsg = "x264出错: {0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string x264ErrorSmr = "x264出错";

        public const string x265ErrorMsg = "x265出错: {0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string x265ErrorSmr = "x265出错";

        public const string svtav1ErrorMsg = "svt-av1出错: {0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string svtav1ErrorSmr = "svt-av1出错";

        public const string vpyErrorMsg = "vpy出错: {0}。\n该文件{1}将跳过处理。请转告技术总监复查。";
        public const string vpyErrorSmr = "vpy出错";

        public const string mmgErrorMsg = "mkvmerge出错: {0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string mmgErrorSmr = "mkvmerge出错";

        public const string lsmashErrorMsg = "l-smash出错: {0}。该文件{1}将跳过处理。请转告技术总监复查。";
        public const string lsmashErrorSmr = "l-smash出错";

        public const string unknownErrorMsg = "未知错误。该文件{0}将跳过处理。请转告技术总监复查。";
        public const string unknownErrorSmr = "未知错误";

        public const string vsCrashMsg = "压制未能完成，预计是vs崩溃。该文件{0}将跳过处理，半成品以HEVC形式保留在目录中。请转告技术总监复查。";
        public const string vsCrashSmr = "vs崩溃";

        public const string x264CrashMsg = "压制未能完成，预计是x264崩溃。该文件{0}将跳过处理，如果是MKV输出，半成品以_.mkv形式保留在目录中。请转告技术总监复查。";
        public const string x264CrashSmr = "x264崩溃";

        public const string x265CrashMsg = "压制未能完成，预计是x265崩溃。该文件{0}将跳过处理，半成品以HEVC形式保留在目录中。请转告技术总监复查。";
        public const string x265CrashSmr = "x265崩溃";

        public const string svtav1CrashMsg = "压制未能完成，预计是svt-av1崩溃。该文件{0}将跳过处理，半成品以HEVC形式保留在目录中。请转告技术总监复查。";
        public const string svtav1CrashSmr = "svt-av1崩溃";

        public const string qaacErrorMsg = "QAAC无法正常运行。请确保你安装了Apple Application Support 64bit";
        public const string qaacErrorSmr = "QAAC无法运行";

        public const string audioFormatMistachMsg = "无法将{0}格式的音轨转为{1}格式。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string audioFormatMistachSmr = "音轨格式不匹配";

        public const string rpcErrorMsg = "RPC出错: {0}。\n请手动检查{1}，并请转告技术总监复查。";
        public const string rpcErrorSmr = "RPC出错";

        public const string reEncodeSliceErrorMsg = "切片不合法: {0}，视频长度为{1}。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string reEncodeSliceErrorSmr = "切片不合法";

        public const string reEncodeFramesErrorMsg = "帧数错误: 脚本输出帧数为{0}，但旧版压制成品帧数为{1}。该文件{2}将跳过处理。请转告技术总监复查。";
        public const string reEncodeFramesErrorSmr = "帧数错误";

        //Application configuration file
        public const string configFile = "OKEGuiConfig.json";

        //Input and Debug, Memory regex
        public static readonly Regex inputRegex = new Regex("# *OKE:INPUTFILE([\\s]+\\w+[ ]*=[ ]*)([r]*[\"'].*[\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static readonly Regex projectDirRegex = new Regex("# *OKE:PROJECTDIR([\\s]+\\w+[ ]*=[ ]*)([r]*[\"'].*[\"'])", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static readonly Regex memoryRegex = new Regex("# *OKE:MEMORY([\\s]+core.max_cache_size+[ ]*=[ ]*)(\\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static readonly Regex debugRegex = new Regex("# *OKE:DEBUG([\\s]+\\w+[ ]*=[ ]*)(\\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        //Deprecated option list
        public static readonly List<string> deprecatedOptions = new List<string> { "SkipMuxing", "IncludeSub", "SubtitleLanguage" };

    }
}
