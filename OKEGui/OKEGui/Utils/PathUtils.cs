using System.IO;

namespace OKEGui
{
    class PathUtils
    {
        public static string GetFullPath(string rel, string baseDir)
        {
            if (Path.IsPathRooted(rel))
                return rel;
            return Path.Combine(baseDir, rel);
        }
    }
}
