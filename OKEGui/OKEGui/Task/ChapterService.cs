using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OKEGui.Utils;
using OKEGui.Model;
using TChapter.Chapters;
using TChapter.Parsing;
using MediaInfo;

namespace OKEGui
{
    public enum ChapterStatus
    {
        No,
        Yes,
        Added,
        Maybe,
        MKV,
        Warn
    };

    public class ChapterService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("ChapterService");

        public static ChapterStatus UpdateChapterStatus(TaskDetail task)
        {
            return HasChapterFile(task) ? ChapterStatus.Yes :
                HasBlurayStructure(task) ? ChapterStatus.Maybe :
                HasMatroskaChapter(task) ? ChapterStatus.MKV : ChapterStatus.No;
        }

        private static bool HasChapterFile(TaskDetail task)
        {
            return FindChapterFile(task);
        }

        private static bool HasBlurayStructure(TaskDetail task)
        {
            FileInfo inputFile = new FileInfo(task.InputFile);
            if (inputFile.Extension.ToLower() != ".m2ts")
            {
                Logger.Warn($"{task.InputFile}不是蓝光原盘文件。");
                return false;
            }

            if (inputFile.Directory.Name.ToUpper() != "STREAM")
            {
                Logger.Warn($"{task.InputFile}不在BDMV文件夹结构内。");
                return false;
            }

            DirectoryInfo playlist = new DirectoryInfo(Path.Combine(inputFile.Directory.Parent.FullName, "PLAYLIST"));
            if (!playlist.Exists)
            {
                Logger.Warn($"{task.InputFile}没有上级的PLAYLIST文件夹");
                return false;
            }

            return playlist.GetFiles("*.mpls").Length > 0;
        }

        private static bool HasMatroskaChapter(TaskDetail task)
        {
            FileInfo inputFile = new FileInfo(task.InputFile);
            if (inputFile.Extension.ToLower() != ".mkv")
            {
                Logger.Warn($"{task.InputFile}不是Matroska文件。");
                return false;
            }

            MediaInfo.MediaInfo MI = new MediaInfo.MediaInfo();
            MI.Open(inputFile.FullName);
            MI.Option("Complete");
            int.TryParse(MI.Get(StreamKind.General, 0, "MenuCount"), out var MenuCount);

            if (MenuCount == 0)
            {
                Logger.Warn($"{task.InputFile}内不含有章节。");
                MI?.Close();
                return false;
            }

            MI?.Close();
            return true;
        }

        public static bool FindChapterFile(TaskDetail task)
        {
            if (!string.IsNullOrEmpty(task.ChapterFileName) && File.Exists(task.ChapterFileName))
                return true;
            FileInfo inputFile = new FileInfo(task.InputFile);
            string inputPath = Path.GetFullPath(inputFile.FullName);
            string basename = Path.GetFileNameWithoutExtension(inputFile.FullName);
            string[] files = Directory.GetFiles(Path.GetDirectoryName(inputPath), basename + ".*txt");
            if (files.Length > 0)
                Logger.Warn($"ChapterFile: found {String.Join(",", files)}.");
            if (files.Length > 1)
                throw new Exception("More than one chapter files found for " + task.InputFile + ": " + String.Join(",", files));
            if (files.Length == 1)
            {
                task.ChapterFileName = files[0];
                string ext = Path.GetFileNameWithoutExtension(task.ChapterFileName);
                if (ext.Length > basename.Length)
                    task.ChapterLanguage = ext.Substring(basename.Length + 1);
                Logger.Warn($"ChapterFile {task.ChapterFileName}, language \"{task.ChapterLanguage}\".");
                return true;
            }
            return false;
        }

        public static ChapterInfo LoadChapter(TaskDetail task)
        {
            FileInfo inputFile = new FileInfo(task.InputFile);
            ChapterInfo chapterInfo;
            switch (task.ChapterStatus)
            {
                case ChapterStatus.Yes:
                    if (!FindChapterFile(task))
                    {
                        throw new Exception($"{task.InputFile} 检测到使用外挂章节，但压制时未找到对应章节 {task.ChapterFileName}");
                    }
                    chapterInfo = new OGMParser().Parse(task.ChapterFileName).FirstOrDefault();
                    break;
                case ChapterStatus.Maybe:
                    DirectoryInfo playlistDirectory =
                        new DirectoryInfo(Path.Combine(inputFile.Directory.Parent.FullName, "PLAYLIST"));
                    chapterInfo = GetChapterFromMPLS(playlistDirectory.GetFiles("*.mpls"), inputFile);
                    break;
                case ChapterStatus.MKV:
                    FileInfo mkvExtract = new FileInfo(".\\tools\\mkvtoolnix\\mkvextract.exe");
                    chapterInfo = new MATROSKAParser(mkvExtract.FullName).Parse(inputFile.FullName).FirstOrDefault();
                    break;
                default:
                    return null;
            }

            if (chapterInfo == null) return null;

            // 丢弃末尾1秒的章节
            chapterInfo.Chapters.Sort((a, b) => a.Time.CompareTo(b.Time));
            chapterInfo.Chapters = chapterInfo.Chapters
                .Where(x => task.LengthInMiliSec - x.Time.TotalMilliseconds > 1001).ToList();

            // 处理开头的重复章节（一般来自mkvmerge切割）
            bool removeBegin = false;
            if (chapterInfo.Chapters.Count >= 2 && chapterInfo.Chapters[0].Time.Ticks == 0 && 
                    chapterInfo.Chapters[1].Time.TotalMilliseconds - chapterInfo.Chapters[0].Time.TotalMilliseconds < 100)
            {
                chapterInfo.Chapters.RemoveAt(1);
                removeBegin = true;
            }

            // 章节重命名
            if (task.Taskfile.RenumberChapters || removeBegin)
            {
                for (int i = 0; i < chapterInfo.Chapters.Count; i++)
                {
                    chapterInfo.Chapters[i].Name = string.Format("Chapter {0,2:0#}", i+1);
                }
                task.ChapterLanguage = "en";
            }

            if (task.ChapterStatus == ChapterStatus.Yes && removeBegin)
            {
                Logger.Warn($"{task.InputFile} 使用外挂章节，但触发了开头章节去重，这可能导致章节内容和语言不符合预期，请注意检查。");
            }

            // 章节序号重排序
            // 对应章节文件中 CHAPTERxx= / CHAPTERxxNAME=
            chapterInfo.UpdateInfo(0);

            if (chapterInfo.Chapters.Count > 1 ||
                chapterInfo.Chapters.Count == 1 && chapterInfo.Chapters[0].Time.Ticks > 0)
            {
                double lastChapterInMiliSec = chapterInfo.Chapters[chapterInfo.Chapters.Count - 1].Time.TotalMilliseconds;
                if (task.LengthInMiliSec - lastChapterInMiliSec < 3003)
                {
                    task.ChapterStatus = ChapterStatus.Warn;
                }
                if (chapterInfo.Chapters[0].Time.Ticks != 0)
                {
                    task.ChapterStatus = ChapterStatus.Warn;
                }
                return chapterInfo;
            }

            Logger.Info(inputFile.Name + "对应章节为空，跳过封装。");
            return null;
        }

        private static ChapterInfo GetChapterFromMPLS(IEnumerable<FileInfo> playlists, FileInfo inputFile)
        {
            MPLSParser parser = new MPLSParser();
            foreach (FileInfo playlistFile in playlists)
            {
                IChapterData allChapters = parser.Parse(playlistFile.FullName);
                foreach (ChapterInfo chapter in allChapters)
                {
                    if (chapter.SourceName + ".m2ts" == inputFile.Name)
                    {
                        return chapter;
                    }
                }
            }

            return null;
        }

        public static IFrameInfo GetChapterIFrameInfo(ChapterInfo chapterInfo, double fps)
        {
            IFrameInfo iFrameInfo = new IFrameInfo();

            foreach (var chapter in chapterInfo.Chapters)
            {
                long miliSec = (long)chapter.Time.TotalMilliseconds;
                long frameNo = (long)(miliSec / 1000.0 * fps + 0.5);
                iFrameInfo.Add(frameNo);
            }

            return iFrameInfo;
        }

        public static IFrameInfo GetChapterIFrameInfo(ChapterInfo chapterInfo, Timecode timecode)
        {
            IFrameInfo iFrameInfo = new IFrameInfo();

            foreach (var chapter in chapterInfo.Chapters)
            {
                iFrameInfo.Add(timecode.GetFrameNumberFromTimeSpan(chapter.Time));
            }

            return iFrameInfo;
        }

        public static string GenerateQpFile(IFrameInfo chapterIFrameInfo)
        {
            StringBuilder qpFile = new StringBuilder();

            foreach (var frame in chapterIFrameInfo)
            {
                qpFile.AppendLine($"{frame} I");
            }

            return qpFile.ToString();
        }
    }
}
