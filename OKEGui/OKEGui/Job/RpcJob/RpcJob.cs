using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OKEGui.JobProcessor;

namespace OKEGui
{
    public class RpcJob : Job
    {
        public readonly string RippedFile;
        public readonly long TotalFrame;
        public readonly Dictionary<string, string> Args = new Dictionary<string, string>();
        public readonly string FailedRPCOutputFile;
        public RpcStatus RpcStatus
        {
            set
            {
                if (ts != null)
                {
                    ts.RpcStatus = value.ToString();
                }
            }
        }
        public RpcJob(string sourceFile, string outputPath, string rippedVideo, long numFrame, List<string> vspipeArgs)
        {
            Input = sourceFile;
            Output = Path.ChangeExtension(sourceFile, "rpc");
            FailedRPCOutputFile = outputPath + ".rpc";
            RippedFile = rippedVideo;
            TotalFrame = numFrame;
            foreach (string arg in vspipeArgs)
            {
                int pos = arg.IndexOf('=');
                string variable = arg.Substring(0, pos);
                string value = arg.Substring(pos + 1);
                Args[variable] = value;
            }
        }
        public override JobType GetJobType()
        {
            return JobType.RpCheck;
        }
    }
}
