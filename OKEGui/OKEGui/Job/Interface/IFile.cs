using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OKEGui
{
    public interface IFile
    {
        /// <summary>
        /// 获取所在目录名
        /// </summary>
        /// <returns>所在目录全路径</returns>
        string GetDirectory();

        /// <summary>
        /// 获取文件名，包含拓展名
        /// </summary>
        /// <returns>文件名</returns>
        string GetFileName();

        /// <summary>
        /// 获取文件拓展名
        /// </summary>
        /// <returns>文件拓展名</returns>
        string GetExtension();

        /// <summary>
        /// 获取不包含拓展名的文件名
        /// </summary>
        /// <returns>文件名</returns>
        string GetFileNameWithoutExtension();

        /// <summary>
        /// 获取文件全路径
        /// </summary>
        /// <returns>文件全路径</returns>
        string GetFullPath();

        /// <summary>
        /// 检查文件路径字符是否安全
        /// </summary>
        /// <returns></returns>
        bool IsPathCharSave();

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <returns>文件大小</returns>
        long GetFileSize();

        /// <summary>
        /// 复制文件到目标目录。不允许覆盖目标目录下文件。
        /// </summary>
        /// <param name="dstPath">目标目录</param>
        /// <returns>复制成功返回新文件。失败返回null。</returns>
        IFile CopyTo(string dstDirectory);

        /// <summary>
        /// 复制文件到目标目录。
        /// </summary>
        /// <param name="dstDirectory">目标目录</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns>复制成功返回新文件。没有文件被复制或者失败返回null。</returns>
        IFile CopyTo(string dstDirectory, bool overwrite);

        /// <summary>
        /// 移动文件到目标目录。不允许覆盖目标目录下文件。
        /// </summary>
        /// <param name="dstPath">目标目录</param>
        /// <returns></returns>
        bool MoveTo(string dstDirectory);

        /// <summary>
        /// 移动文件到目标目录。
        /// </summary>
        /// <param name="dstDirectory">目标目录</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        bool MoveTo(string dstDirectory, bool overwrite);

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <returns></returns>
        bool Delete();

        /// <summary>
        /// 重命名文件。不允许覆盖。
        /// </summary>
        /// <param name="newName">新文件名</param>
        /// <returns></returns>
        bool Rename(string newName);

        /// <summary>
        /// 重命名文件。
        /// </summary>
        /// <param name="newName">新文件名</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        bool Rename(string newName, bool overwrite);

        /// <summary>
        /// 更改文件拓展名。不允许覆盖。
        /// </summary>
        /// <param name="newExt">新拓展名。e.g. ".bak"</param>
        /// <returns></returns>
        bool ChangeExtension(string newExt);

        /// <summary>
        /// 更改文件拓展名。
        /// </summary>
        /// <param name="newExt">新拓展名。e.g. ".bak"</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        bool ChangeExtension(string newExt, bool overwrite);

        /// <summary>
        /// 添加文件拓展名。不允许覆盖。
        /// </summary>
        /// <param name="newExt">新拓展名。e.g. ".bak"</param>
        /// <returns></returns>
        bool AddExtension(string newExt);

        /// <summary>
        /// 添加文件拓展名。
        /// </summary>
        /// <param name="newExt">新拓展名。e.g. ".bak"</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <returns></returns>
        bool AddExtension(string newExt, bool overwrite);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <returns></returns>
        bool Exists();
    }

    public class OKEFile : IFile
    {
        private FileInfo fi;

        public OKEFile(string path)
        {
            fi = new FileInfo(path);
        }

        public OKEFile(FileInfo fileInfo)
        {
            fi = fileInfo;
        }

        public bool ChangeExtension(string newExt)
        {
            return this.Rename(Path.ChangeExtension(fi.FullName, newExt));
        }

        public bool ChangeExtension(string newExt, bool overwrite)
        {
            return this.Rename(Path.ChangeExtension(fi.FullName, newExt), overwrite);
        }

        public bool AddExtension(string newExt)
        {
            return this.Rename(fi.FullName + newExt);
        }

        public bool AddExtension(string newExt, bool overwrite)
        {
            return this.Rename(fi.FullName + newExt, overwrite);
        }

        public IFile CopyTo(string dstDirectory)
        {
            try {
                return new OKEFile(fi.CopyTo(dstDirectory + this.GetFileName()));
            } catch (Exception) {
                return null;
            }
        }

        public IFile CopyTo(string dstDirectory, bool overwrite)
        {
            if (!overwrite && new FileInfo(dstDirectory + this.GetFileName()).Exists) {
                // 文件已经存在且不覆盖
                return null;
            }

            try {
                return new OKEFile(fi.CopyTo(dstDirectory + this.GetFileName()));
            } catch (Exception) {
                return null;
            }
        }

        public bool Delete()
        {
            try {
                File.Delete(fi.FullName);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public string GetDirectory()
        {
            return fi.Directory.FullName;
        }

        public string GetExtension()
        {
            return fi.Extension;
        }

        public string GetFileName()
        {
            return fi.Name;
        }

        public string GetFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(fi.FullName);
        }

        public long GetFileSize()
        {
            return fi.Length;
        }

        public string GetFullPath()
        {
            return fi.FullName;
        }

        public bool IsPathCharSave()
        {
            const string pattern = "[a-zA-Z]:(\\\\([\\&\\[\\]\\ 0-9a-zA-Z-]+))+(\\.?)([a-zA-Z0-9]*)";

            return Regex.Match(fi.FullName, pattern).Value == fi.FullName;
        }

        public bool MoveTo(string dstDirectory)
        {
            try {
                fi.MoveTo(dstDirectory + fi.Name);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public bool MoveTo(string dstDirectory, bool overwrite)
        {
            if (!overwrite && new FileInfo(dstDirectory + this.GetFileName()).Exists) {
                // 文件已经存在且不覆盖
                return false;
            }

            try {
                fi.MoveTo(dstDirectory + fi.Name);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public bool Rename(string newName)
        {
            try {
                fi.MoveTo(newName);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public bool Rename(string newName, bool overwrite)
        {
            if (!overwrite && new FileInfo(newName).Exists) {
                // 文件已经存在且不覆盖
                return false;
            }

            try {
                fi.MoveTo(newName);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public bool Exists()
        {
            return fi.Exists;
        }
    }
}
