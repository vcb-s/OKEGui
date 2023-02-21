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

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return errorMsg;
            }
            else
            {
                return fileName + " : " + errorMsg;
            }
        }
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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("ExceptionParser");

        public static ExceptionMsg Parse(OKETaskException ex, TaskDetail task)
        {
            Logger.Warn("收到异常" + ex.StackTrace);
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
                case Constants.audioNumMismatchSmr:
                    msg.errorMsg = string.Format(Constants.audioNumMismatchMsg, ex.Data["SRC_TRACK"], ex.Data["DST_REQ_TRACK"], ex.Data["DST_OPT_TRACK"], task.InputFile);
                    break;

                case Constants.subNumMismatchSmr:
                    msg.errorMsg = string.Format(Constants.subNumMismatchMsg, ex.Data["SRC_TRACK"], ex.Data["DST_REQ_TRACK"], ex.Data["DST_OPT_TRACK"], task.InputFile);
                    break;

                case Constants.fpsMismatchSmr:
                    msg.errorMsg = string.Format(Constants.fpsMismatchMsg, ex.Data["SRC_FPS"], ex.Data["DST_FPS"], task.InputFile);
                    break;

                case Constants.x264ErrorSmr:
                    msg.errorMsg = string.Format(Constants.x264ErrorMsg, ex.Data["X264_ERROR"], task.InputFile);
                    break;

                case Constants.x265ErrorSmr:
                    msg.errorMsg = string.Format(Constants.x265ErrorMsg, ex.Data["X265_ERROR"], task.InputFile);
                    break;

                case Constants.svtav1ErrorSmr:
                    msg.errorMsg = string.Format(Constants.svtav1ErrorMsg, ex.Data["SVTAV1_ERROR"], task.InputFile);
                    break;

                case Constants.vpyErrorSmr:
                    msg.errorMsg = string.Format(Constants.vpyErrorMsg, ex.Data["VPY_ERROR"], task.InputFile);
                    break;

                case Constants.vsCrashSmr:
                    msg.errorMsg = string.Format(Constants.vsCrashMsg, task.InputFile);
                    break;

                case Constants.x264CrashSmr:
                    msg.errorMsg = string.Format(Constants.x264CrashMsg, task.InputFile);
                    break;

                case Constants.x265CrashSmr:
                    msg.errorMsg = string.Format(Constants.x265CrashMsg, task.InputFile);
                    break;

                case Constants.svtav1CrashSmr:
                    msg.errorMsg = string.Format(Constants.svtav1CrashMsg, task.InputFile);
                    break;

                case Constants.qaacErrorSmr:
                    msg.errorMsg = string.Format(Constants.qaacErrorMsg);
                    break;

                case Constants.audioFormatMistachSmr:
                    msg.errorMsg = string.Format(Constants.audioFormatMistachMsg, ex.Data["SRC_FMT"], ex.Data["DST_FMT"], task.InputFile);
                    break;

                case Constants.rpcErrorSmr:
                    msg.errorMsg = string.Format(Constants.rpcErrorMsg, ex.Data["RPC_ERROR"], task.InputFile);
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
