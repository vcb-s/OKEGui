using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using OKEGui.Utils;

namespace OKEGui.JobProcessor
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

    public enum RpcStatus
    {
        等待中,
        跳过,
        错误,
        未通过,
        通过
    };

    public class RpChecker : CommandlineJobProcessor
    {
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
        private long frameCount = 0;
        private bool isVSError = false;
        private string errorMsg;

        protected RpcJob RJob
        {
            get { return job as RpcJob; }
        }

        public RpChecker(RpcJob rjob) : base(rjob)
        {
            executable = Initializer.Config.vspipePath;
            commandLine = $" \"{GetRpcScript()}\" .";
            result.FileNamePair = (RJob.Input, RJob.RippedFile);
        }

        public string GetRpcScript()
        {
            string argsClauses = "";
            foreach (KeyValuePair<string, string> itr in RJob.Args)
            {
                argsClauses += $"setattr(mod, '{itr.Key}', b'{itr.Value}')" + Environment.NewLine;
            }
            string scriptContent = File.ReadAllText(TemplateFile);
            scriptContent = scriptContent
                .Replace("OKE:SOURCE_SCRIPT", RJob.Input)
                .Replace("OKE:VIDEO_FILE", RJob.RippedFile)
                .Replace("OKE:VSPIPE_ARGS", argsClauses);
            string fileName = RJob.RippedFile.Replace(Path.GetExtension(RJob.RippedFile), "_rpc.vpy");
            File.WriteAllText(fileName, scriptContent);

            return fileName;
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);
            if (line.Contains("Python exception: "))
            {
                isVSError = true;
                errorMsg = "";
            }
            else if (isVSError)
            {
                Regex rExit = new Regex("^([a-zA-Z]*)(Error|Exception|Exit|Interrupt|Iteration|Warning)");
                if (rExit.IsMatch(line))
                {
                    string[] match = rExit.Split(line);
                    Logger.Error(match[1] + match[2]);

                    errorMsg += "\n" + line;
                    status = RpcStatus.错误;
                    finishMre.Set();
                    OKETaskException ex = new OKETaskException(Constants.rpcErrorSmr);
                    ex.Data["RPC_ERROR"] = errorMsg;
                    throw ex;
                }
                else if (line != "")
                {
                    errorMsg += "\n" + line;
                }
            }
            else if (line.Contains("RPCOUT:"))
            {
                frameCount++;
                RJob.Progress = 100.0 * frameCount / RJob.TotalFrame;
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
            RJob.RpcStatus = Status;
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.None
            };
            if (Status != RpcStatus.通过)
            {
                RJob.Output = RJob.FailedRPCOutputFile;
            }
            RJob.Output = RJob.Output.Replace(".rpc", $"-{Status}.rpc");
            using (StreamWriter fileWriter = new StreamWriter(RJob.Output))
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
