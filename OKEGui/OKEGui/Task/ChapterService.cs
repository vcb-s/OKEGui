using OKEGui.JobProcessor;
using OKEGui.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TChapter.Chapters;
using TChapter.Parsing;

namespace OKEGui
{
    public class ChapterService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public enum ChapterStatus { No, Yes, Added };

        public static SortedDictionary<string, string> ReadChapters(OKEFile file)
        {
            SortedDictionary<string, string> chapters = new SortedDictionary<string, string>();
            string fileContent = File.ReadAllText(file.GetFullPath());
            string[] chapterLinesArr = fileContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            List<string> chapterLines = new List<string>(chapterLinesArr);
            chapterLines.RemoveAll(i => string.IsNullOrWhiteSpace(i));

            for (int i = 0; i < chapterLines.Count / 2; i++)
            {
                string strTime = chapterLines[i + i].Split(new char[] { '=' })[1];
                string name = chapterLines[i + i + 1].Split(new char[] { '=' })[1];
                chapters.Add(strTime, name);
            }
            return chapters;
        }

        public static long StrToMilisec(string str)
        {
            string[] hms = str.Split(new char[] { ':' });
            if (hms.Length != 3)
            {
                throw new ArgumentException(str + "无法识别为时间！");
            }
            long h = long.Parse(hms[0]);
            long m = long.Parse(hms[1]);
            double s = double.Parse(hms[2]);
            return h * 3600000 + m * 60000 + (long)(s * 1000 + 0.5);
        }

        public static string UpdateChapterStatus(TaskDetail task)
        {
            bool hasChapter = HasChapterFile(task);
            if (hasChapter)
            {
                return ChapterStatus.Yes.ToString();
            }

            hasChapter = GetChapterFromMPLS(task);
            if (hasChapter)
            {
                return ChapterStatus.Added.ToString();
            }
            else
            {
                return ChapterStatus.No.ToString();
            }
        }
        public static bool HasChapterFile(TaskDetail task)
        {
            FileInfo txtChapter = new FileInfo(Path.ChangeExtension(task.InputFile, ".txt"));
            return txtChapter.Exists;
        }

        public static OKEFile AddChapter(TaskDetail task)
        {
            FileInfo txtChapter = new FileInfo(Path.ChangeExtension(task.InputFile, ".txt"));
            if (txtChapter.Exists)
            {
                OKEFile chapterFile = new OKEFile(txtChapter);
                ChapterChecker checker = new ChapterChecker(chapterFile, task.lengthInMiliSec);
                checker.RemoveUnnecessaryEnd();

                if (checker.IsEmpty())
                {
                    Logger.Info(txtChapter.Name + "为空，跳过封装。");
                    return null;
                }
                else
                {
                    task.MediaOutFile.AddTrack(new ChapterTrack(chapterFile));
                    return chapterFile;
                }
            }

            return null;
        }

        public static bool GetChapterFromMPLS(TaskDetail task)
        {
            if (!task.InputFile.EndsWith(".m2ts"))
            {
                Logger.Warn($"{ task.InputFile }不是蓝光原盘文件。");
                return false;
            }

            FileInfo inputFile = new FileInfo(task.InputFile);
            string folder = inputFile.DirectoryName;

            if (!folder.EndsWith(@"\BDMV\STREAM"))
            {
                Logger.Warn($"{ task.InputFile }不在BDMV文件夹结构内。");
                return false;
            }
            folder = folder.Remove(folder.Length - 6);
            folder += "PLAYLIST";
            DirectoryInfo playlist = new DirectoryInfo(folder);

            if (!playlist.Exists)
            {
                Logger.Warn($"{ task.InputFile }没有上级的PLAYLIST文件夹");
                return false;
            }

            FileInfo[] allPlaylists = playlist.GetFiles("*.mpls");
            MPLSParser parser = new MPLSParser();

            foreach (FileInfo mplsFile in allPlaylists)
            {
                IChapterData allChapters = parser.Parse(mplsFile.FullName);
                for (int i = 0; i < allChapters.Count; i++)
                {
                    ChapterInfo chapter = allChapters[i];
                    if (chapter.SourceName + ".m2ts" == inputFile.Name)
                    {
                        //save chapter file
                        string chapterFileName = Path.ChangeExtension(task.InputFile, ".txt");
                        allChapters.Save(ChapterTypeEnum.OGM, chapterFileName, i);
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GenerateQpFile(OKEFile chapterFile, double fps)
        {
            string qpFile = "";

            SortedDictionary<string, string> chapters = ReadChapters(chapterFile);
            foreach (string strTimeStamp in chapters.Keys)
            {
                long miliSec = StrToMilisec(strTimeStamp);
                int frameNo = (int)(miliSec / 1000.0 * fps + 0.5);
                qpFile += frameNo.ToString() + " I" + Environment.NewLine;
            }

            return qpFile;
        }
    }
}
