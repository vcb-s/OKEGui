using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OKEGui.RpChecker;

namespace OKEGui
{
    public class RpcJob : Job
    {
        public readonly string RippedFile;
        public readonly ulong TotalFrame;
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
        public RpcJob(string sourceFile, VideoJob videoJob, string outputPath)
        {
            Input = sourceFile;
            Output = Path.ChangeExtension(sourceFile, "rpc");
            RippedFile = videoJob.Output;
            FailedRPCOutputFile = outputPath + ".rpc";
            TotalFrame = videoJob.NumberOfFrames;
            foreach (string arg in videoJob.VspipeArgs)
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
