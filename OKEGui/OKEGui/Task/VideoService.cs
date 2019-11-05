using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui
{
    /* miscellaneous functions for video jobs:
     * * bool ForceExtractVideo: check whether the m2ts file contains 0x47 in its first 4 bytes. 
     * If so, it must be extracted before encoding.
     * 
     * * void ReplaceVpyInputFile: modify the vapoursynth script, replace the original filename by new name. 
     */
    public class VideoService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static bool ForceExtractVideo(string path)
        {
            if (!path.ToLower().EndsWith(".m2ts"))
            {
                return false;
            }

            byte[] buffer = new byte[4];

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, 4);
                fs.Close();
            }

            uint header = 0;
            foreach (byte i in buffer)
            {
                header = (header << 8) + i;
            }

            string strHeader = header.ToString("X8");
            Logger.Debug($"{path}前4位是{strHeader}");

            for (int i = 0; i < 8; i += 2)
            {
                if (strHeader.Substring(i, 2) == "47")
                {
                    return true;
                }
            }

            return false;
        }

        public static void ReplaceVpyInputFile(string vpyPath, string originalInput, string newInput)
        {
            string contents = File.ReadAllText(vpyPath);
            contents = contents.Replace(originalInput, newInput);
            File.WriteAllText(vpyPath, contents);
        }
    }
}
