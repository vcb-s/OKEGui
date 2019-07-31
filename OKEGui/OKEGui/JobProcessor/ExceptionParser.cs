using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OKEGui.Utils;
using OKEGui;
using System.IO;

namespace OKEGui.JobProcessor
{
    struct ExceptionMsg
    {
        public string errorMsg;
        public string fileName;
    }

    public class OKETaskException : OperationCanceledException
    {
        public readonly string summary;
        public double? progress = null;

        public OKETaskException()
        {
            summary = Constants.unknownErrorSmr;
        }

        public OKETaskException(string message)
            : base(message)
        {
            summary = message;
        }

        public OKETaskException(string message, Exception inner)
            : base(message, inner)
        {
            summary = message;
        }
    }

    static class ExceptionParser
    {
        public static ExceptionMsg parse(OKETaskException ex, TaskDetail task)
        {
            ExceptionMsg msg;
            if (task != null)
            {
                FileInfo fileinfo = new FileInfo(task.InputFile);
                msg.fileName = fileinfo.Name;
            }
            else
            {
                msg.fileName = "";
            }
            switch (ex.summary)
            {
                case Constants.eac3toMissingSmr:
                    msg.errorMsg = Constants.eac3toMissingMsg;
                    break;

                case Constants.audioNumMismatchSmr:
                    msg.errorMsg = string.Format(Constants.audioNumMismatchMsg, ex.Data["SRC_TRACK"], ex.Data["DST_TRACK"], task.InputFile);
                    break;

                case Constants.fpsMismatchSmr:
                    msg.errorMsg = string.Format(Constants.fpsMismatchMsg, ex.Data["SRC_FPS"], ex.Data["DST_FPS"], task.InputFile);
                    break;

                case Constants.x265ErrorSmr:
                    msg.errorMsg = string.Format(Constants.x265ErrorMsg, ex.Data["X265_ERROR"], task.InputFile);
                    break;

                case Constants.vpyErrorSmr:
                    msg.errorMsg = string.Format(Constants.vpyErrorMsg, ex.Data["VPY_ERROR"], task.InputFile);
                    break;

                case Constants.vsCrashSmr:
                    msg.errorMsg = string.Format(Constants.vsCrashMsg, task.InputFile);
                    break;

                case Constants.x265CrashSmr:
                    msg.errorMsg = string.Format(Constants.x265CrashMsg, task.InputFile);
                    break;

                case Constants.qaacErrorSmr:
                    msg.errorMsg = string.Format(Constants.qaacErrorMsg);
                    break;

                case Constants.unknownErrorSmr:
                default:
                    msg.errorMsg = string.Format(Constants.unknownErrorMsg, task.InputFile);
                    break;
            }
            return msg;
        }
    }
}
