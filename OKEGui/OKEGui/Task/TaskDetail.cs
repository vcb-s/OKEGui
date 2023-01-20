using System;
using System.Collections.Generic;
using OKEGui.Model;
using OKEGui.Worker;

namespace OKEGui
{
    /// <summary>
    /// 在TaskStatus基础上，继续定义无需显示的域，来整体构成一个Task所需的所有数据和函数。
    /// </summary>
    public class TaskDetail : TaskStatus
    {
        // Task信息。从Json中读入。（见WizardWindow)
        public TaskProfile Taskfile;
        // Task所分解成的Job队列。
        public Queue<Job> JobQueue = new Queue<Job>();

        public string ChapterFileName;
        public string ChapterLanguage;

        // 输出文件轨道。MediaOutFile是主文件(mp4/mkv), MkaOutFile是外挂mka
        public MediaFile MediaOutFile;
        public MediaFile MkaOutFile;

        public string Tid;
        public long LengthInMiliSec;

        // 自动生成输出文件名
        public void UpdateOutputFileName()
        {
            var finfo = new System.IO.FileInfo(InputFile);
            if (Taskfile.ContainerFormat != "")
            {
                OutputFile = finfo.Name + "." + Taskfile.ContainerFormat.ToLower();
            }
            else
            {
                OutputFile = finfo.Name + "." + Taskfile.VideoFormat.ToLower();
            }
        }
    }
}
