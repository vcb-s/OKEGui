using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OKEGui.Utils;
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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static ChapterStatus UpdateChapterStatus(TaskDetail task)
        {
            return HasChapterFile(task) ? ChapterStatus.Yes :
                HasBlurayStructure(task) ? ChapterStatus.Maybe :
                HasMatroskaChapter(task) ? ChapterStatus.MKV : ChapterStatus.No;
        }

        private static bool HasChapterFile(TaskDetail task)
        {
            FileInfo txtChapter = new FileInfo(Path.ChangeExtension(task.InputFile, ".txt"));
            return txtChapter.Exists;
        }

        private static bool HasBlurayStructure(TaskDetail task)
        {
            FileInfo inputFile = new FileInfo(task.InputFile);
            if (inputFile.Extension != ".m2ts")
            {
                Logger.Warn($"{task.InputFile}不是蓝光原盘文件。");
                return false;
            }

            if (inputFile.Directory.Name != "STREAM")
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
            if (inputFile.Extension != ".mkv")
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

        public static ChapterInfo LoadChapter(TaskDetail task)
        {
            FileInfo inputFile = new FileInfo(task.InputFile);
            ChapterInfo chapterInfo;
            switch (task.ChapterStatus)
            {
                case ChapterStatus.Yes:
                {
                    FileInfo txtChapter = new FileInfo(Path.ChangeExtension(inputFile.FullName, ".txt"));
                    if (!txtChapter.Exists) return null;
                    chapterInfo = new OGMParser().Parse(txtChapter.FullName).FirstOrDefault();
                    break;
                }
                case ChapterStatus.Maybe:
                {
                    DirectoryInfo playlistDirectory =
                        new DirectoryInfo(Path.Combine(inputFile.Directory.Parent.FullName, "PLAYLIST"));
                    chapterInfo = GetChapterFromMPLS(playlistDirectory.GetFiles("*.mpls"), inputFile);
                    break;
                }
                case ChapterStatus.MKV:
                    {
                        FileInfo mkvExtract = new FileInfo(".\\tools\\mkvtoolnix\\mkvextract.exe");
                        chapterInfo = new MATROSKAParser(mkvExtract.FullName).Parse(inputFile.FullName).FirstOrDefault();
                        break;
                    }
                default:
                    return null;
            }

            if (chapterInfo == null) return null;

            chapterInfo.Chapters.Sort((a, b) => a.Time.CompareTo(b.Time));
            chapterInfo.Chapters = chapterInfo.Chapters
                .Where(x => task.LengthInMiliSec - x.Time.TotalMilliseconds > 1001).ToList();
            if (chapterInfo.Chapters.Count > 1 ||
                chapterInfo.Chapters.Count == 1 && chapterInfo.Chapters[0].Time.Ticks > 0)
            {
                double lastChapterInMiliSec = chapterInfo.Chapters[chapterInfo.Chapters.Count - 1].Time.TotalMilliseconds;
                if (task.LengthInMiliSec - lastChapterInMiliSec < 3003)
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

        public static string GenerateQpFile(ChapterInfo chapterInfo, double fps)
        {
            StringBuilder qpFile = new StringBuilder();

            foreach (var chapter in chapterInfo.Chapters)
            {
                long miliSec = (long) chapter.Time.TotalMilliseconds;
                int frameNo = (int) (miliSec / 1000.0 * fps + 0.5);
                qpFile.AppendLine($"{frameNo} I");
            }

            return qpFile.ToString();
        }

        public static string GenerateQpFile(ChapterInfo chapterInfo, Timecode timecode)
        {
            StringBuilder qpFile = new StringBuilder();

            foreach (var chapter in chapterInfo.Chapters)
            {
                qpFile.AppendLine($"{timecode.GetFrameNumberFromTimeSpan(chapter.Time)} I");
            }

            return qpFile.ToString();
        }
    }
}
