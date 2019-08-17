using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OKEGui.Model
{
    public class OKEFile
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

        public OKEFile CopyTo(string dstDirectory)
        {
            try {
                return new OKEFile(fi.CopyTo(dstDirectory + this.GetFileName()));
            } catch (Exception) {
                return null;
            }
        }

        public OKEFile CopyTo(string dstDirectory, bool overwrite)
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

        public bool AddCRC32()
        {
            string CRC32Code = Utils.CRC32.ComputeChecksumString(fi.FullName);
            string newName = GetDirectory() + "\\" + GetFileNameWithoutExtension() + " [" + CRC32Code + "]" + GetExtension();
            return Rename(newName);
        }
    }
}
