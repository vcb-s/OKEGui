using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OKEGui.JobProcessor;
using OKEGui.Utils;

namespace OKEGui
{
    // To be deprecated.
    public class LogBuffer
    {
        public bool Inf = false;
    }
    public class RpcResult
    {
        public List<(int index, double value)> Data = new List<(int index, double value)>();
        public (string src, string opt) FileNamePair;
        public LogBuffer Logs = new LogBuffer();
    }
    public class RpcResult3
    {
        public List<(int index, double value, double valueU, double valueV)> Data = new List<(int index, double value, double valueU, double valueV)>();
        public (string src, string opt) FileNamePair;
        public LogBuffer Logs = new LogBuffer();
    }

    public class RpChecker : CommandlineJobProcessor
    {
        public enum RpcStatus { 等待中, 跳过, 错误, 未通过, 通过 };

        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("RpChecker");
        private static readonly double psnr_threashold = 30.0;
        private static readonly double psnrUV_threshold = 40.0;

        private RpcStatus status = RpcStatus.等待中;
        public RpcStatus Status
        {
            get
            {
                finishMre.WaitOne();
                return status;
            }
            private set
            {
                status = value;
            }
        }

        private RpcResult result = new RpcResult();
        private RpcResult3 result3 = new RpcResult3();

        private const string TemplateFile = ".\\tools\\rpc\\RpcTemplate.vpy";
        private RpcJob job;
        private ulong frameCount = 0;

        public RpChecker(RpcJob job) : base()
        {
            executable = Initializer.Config.vspipePath;
            commandLine = $" \"{GetRpcScript(job)}\" .";
            this.job = job;
            result.FileNamePair = (job.Input, job.RippedFile);
        }

        public string GetRpcScript(RpcJob job)
        {
            string argsClauses = "";
            foreach (KeyValuePair<string, string> itr in job.Args)
            {
                argsClauses += $"setattr(mod, '{itr.Key}', b'{itr.Value}')" + Environment.NewLine;
            }
            string scriptContent = File.ReadAllText(TemplateFile);
            scriptContent = scriptContent
                .Replace("OKE:SOURCE_SCRIPT", job.Input)
                .Replace("OKE:VIDEO_FILE", job.RippedFile)
                .Replace("OKE:VSPIPE_ARGS", argsClauses);
            string fileName = job.RippedFile.Replace(Path.GetExtension(job.RippedFile), "_rpc.vpy");
            File.WriteAllText(fileName, scriptContent);

            return fileName;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);
            if (line.Contains("Python exception: "))
            {
                status = RpcStatus.错误;
                finishMre.Set();
                OKETaskException ex = new OKETaskException(Constants.rpcErrorSmr);
                ex.Data["RPC_ERROR"] = line.Substring(18);
                throw ex;
            }
            else if (line.Contains("RPCOUT:"))
            {
                frameCount++;
                job.Progress = 100.0 * frameCount / job.TotalFrame;
                string[] strNumbers = line.Substring(8).Split(new char[] { ' ' });
                int frameNo = int.Parse(strNumbers[0]);
                double psnr = double.Parse(strNumbers[1]), psnrU = psnrUV_threshold, psnrV = psnrUV_threshold;
                if (strNumbers.Length > 3)
                {
                    psnrU = double.Parse(strNumbers[2]);
                    psnrV = double.Parse(strNumbers[3]);
                    result3.Data.Add((frameNo, psnr, psnrU, psnrV));
                }
                else
                    result.Data.Add((frameNo, psnr));
                if (psnr < psnr_threashold || psnrU < psnrUV_threshold || psnrV < psnrUV_threshold)
                {
                    status = RpcStatus.未通过;
                }
            }
            else if (line.StartsWith("Output "))
            {
                if (status == RpcStatus.等待中)
                {
                    status = RpcStatus.通过;
                }
                finishMre.Set();
            }
        }

        public override void waitForFinish()
        {
            base.waitForFinish();
            job.RpcStatus = Status;
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.None
            };
            if (Status != RpcStatus.通过)
            {
                job.Output = job.FailedRPCOutputFile;
            }
            job.Output = job.Output.Replace(".rpc", $"-{Status.ToString()}.rpc");
            using (StreamWriter fileWriter = new StreamWriter(job.Output))
            using (JsonTextWriter writer = new JsonTextWriter(fileWriter))
            {
                if (result.Data.Count > 0)
                    serializer.Serialize(writer, new RpcResult[] { result });
                else
                    serializer.Serialize(writer, new RpcResult3[] { result3 });
            }
        }
    }
}
