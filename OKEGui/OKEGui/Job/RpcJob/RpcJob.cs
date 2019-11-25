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
        public RpcJob(string sourceFile, string rippedFile, ulong totalFrame)
        {
            Input = sourceFile;
            Output = Path.ChangeExtension(sourceFile, "rpc");
            RippedFile = rippedFile;
            TotalFrame = totalFrame;
        }
        public override JobType GetJobType()
        {
            return JobType.RpCheck;
        }
    }
}
