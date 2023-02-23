using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;


namespace OKEGui.Utils
{
    class Cleaner
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("Cleaner");

        private List<string> sfxRemove, sfxRename;
        private const string TIME_FMT = "ddHHmm";

        public Cleaner() : this(
            new List<string> { "flac", "alac", "aac", "ac3", "dts", "sup", "Log.txt", "vpy", "qpf", "lwi", "rpc", "pyc" },
            new List<string> { "hevc", "mkv", "mp4", "mka", "h264" })
        {
        }
        public Cleaner(List<string> sfxRemove, List<string> sfxRename)
        {
            this.sfxRemove = sfxRemove;
            this.sfxRename = sfxRename;
        }

        public List<string> Clean(string inputFile, List<string> whiteList)
        {
            if (!whiteList.Contains(inputFile))
            {
                whiteList.Add(inputFile);
            }
            List<string> removed = Remove(inputFile, whiteList);
            //List<string> renamed = Rename(inputFile, whiteList);
            //removed.AddRange(renamed);
            return removed;
        }

        public List<string> Remove(string inputFile, List<string> whiteList)
        {
            string directory = Path.GetDirectoryName(inputFile);
            string rawName = Path.GetFileNameWithoutExtension(inputFile);
            List<string> files = new List<string>(Directory.GetFiles(directory, rawName + "*.*", SearchOption.AllDirectories)
            .Where(s => !whiteList.Contains(s) && sfxRemove.Any(x => s.EndsWith(x))));
            foreach (string file in files) {
                File.Delete(file);
            }
            return files;
        }

        public List<string> Rename(string inputFile, List<string> whiteList)
        {
            string directory = Path.GetDirectoryName(inputFile);
            string rawName = Path.GetFileNameWithoutExtension(inputFile);
            List<string> files = new List<string>(Directory.GetFiles(directory, rawName + "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => !whiteList.Contains(s) && sfxRename.Any(x => s.EndsWith(x))));
            DateTime time = DateTime.Now;
            for (int i = 0; i < files.Count; i++)
            {
                string oldFile = files[i];
                rawName = Path.GetFileNameWithoutExtension(oldFile);
                string extension = Path.GetExtension(oldFile);
                string newFile = directory + @"\" + rawName + "_b_" + time.ToString(TIME_FMT) + extension;
                try
                {
                    File.Move(oldFile, newFile);
                    files[i] = newFile;
                }
                catch (Exception)
                {
                    Logger.Error($"无法备份{oldFile}，直接删除。");
                    File.Delete(oldFile);
                }

            }
            return files;
        }
    }
}
