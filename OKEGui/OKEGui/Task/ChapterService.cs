using OKEGui.JobProcessor;
using OKEGui.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui
{
    class ChapterService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string GetChapterStatus(TaskDetail task)
        {
            bool hasChapter = HasChapterFile(task);
            return hasChapter ? "Yes" : "No";
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
    }
}
