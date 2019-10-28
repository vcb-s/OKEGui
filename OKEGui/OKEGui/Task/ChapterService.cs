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

        public static TaskDetail AddChapter(TaskDetail task)
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
                }
                else
                {
                    task.MediaOutFile.AddTrack(new ChapterTrack(chapterFile));
                }
            }
            return task;
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

            FileInfo[] allPlaylists = playlist.GetFiles();
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
    }
}
