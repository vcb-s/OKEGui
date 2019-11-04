using OKEGui.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OKEGui.JobProcessor
{
    public class ChapterChecker
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly OKEFile ChapterFile;
        private readonly long LengthInMiliSec;
        private readonly SortedDictionary<string, string> Chapters;

        public ChapterChecker(OKEFile chapterFile, long lengthInMiliSec)
        {
            ChapterFile = chapterFile;
            LengthInMiliSec = lengthInMiliSec;
            Chapters = ChapterService.ReadChapters(chapterFile);
        }


        private void WriteChapter()
        {
            int idx = 0;
            string allText = "";
            foreach (KeyValuePair<string, string> chapter in Chapters)
            {
                idx++;
                string strIdx = idx.ToString("D2");
                allText += "CHAPTER" + strIdx + "=" + chapter.Key + Environment.NewLine;
                allText += "CHAPTER" + strIdx + "NAME=" + chapter.Value + Environment.NewLine;
            }
            string filePath = ChapterFile.GetFullPath();
            File.Move(filePath, Path.ChangeExtension(filePath, ".bak") + ChapterFile.GetExtension());
            File.WriteAllText(filePath, allText);
        }

        public void RemoveUnnecessaryEnd()
        {
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, string> chapter in Chapters)
            {
                long timeInMiliSec = ChapterService.StrToMilisec(chapter.Key);
                if (timeInMiliSec >= LengthInMiliSec - 1001)
                {
                    Logger.Info(chapter.Value + ":" + chapter.Key + "的时间在文件结尾1秒内，删除。");
                    toRemove.Add(chapter.Key);
                }
            }

            if (toRemove.Count > 0)
            {
                foreach (string key in toRemove)
                {
                    Chapters.Remove(key);
                }
                WriteChapter();
            }
        }

        public bool IsEmpty()
        {
            if (Chapters.ContainsKey("00:00:00.000"))
            {
                return Chapters.Count == 1;
            }
            else
            {
                return Chapters.Count == 0;
            }
        }
    }
}
