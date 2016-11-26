using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OKEGui
{
    public class CMDPipeJobProcessor : IJobProcessor
    {
        public event JobProcessingStatusUpdateCallback StatusUpdate;

        private CommandlineJobProcessor outProcessor, inProcessor;

        Process outProc = new Process();
        Process inProc = new Process();

        ManualResetEvent mre = new ManualResetEvent(false);
        ManualResetEvent pauseMre = new ManualResetEvent(true);

        public static CMDPipeJobProcessor NewCMDPipeJobProcessor(IJobProcessor stdout, IJobProcessor stdin)
        {
            var pipe = new CMDPipeJobProcessor();
            pipe.SetStdOut(stdout as CommandlineJobProcessor);
            pipe.SetStdIn(stdin as CommandlineJobProcessor);

            return pipe;
        }

        public CMDPipeJobProcessor()
        {
        }

        public void SetStdOut(CommandlineJobProcessor cliProcessor)
        {
            outProcessor = cliProcessor;
        }

        public void SetStdIn(CommandlineJobProcessor cliProcessor)
        {
            inProcessor = cliProcessor;
        }

        public void changePriority(ProcessPriority priority)
        {
            outProcessor.changePriority(priority);
            inProcessor.changePriority(priority);
        }

        public void pause()
        {
            if (!mre.Reset())
                throw new Exception("Could not reset mutex. pause failed");
        }

        public void resume()
        {
            if (!mre.Set())
                throw new Exception("Could not set mutex. pause failed");
        }

        public void setup(Job job, StatusUpdate su)
        {
        }

        public void start()
        {
            var startInfo = new ProcessStartInfo {
                FileName = outProcessor.Executable,
                Arguments = outProcessor.Commandline,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                // RedirectStandardError = true,
                // RedirectStandardInput = true,
            };
            outProc.StartInfo = startInfo;

            startInfo = new ProcessStartInfo {
                FileName = inProcessor.Executable,
                Arguments = inProcessor.Commandline,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };
            inProc.StartInfo = startInfo;

            try {
                outProc.Start();
                inProc.Start();

                // 转发线程
                new Thread(new ThreadStart(() => {
                    try {
                        while (!mre.WaitOne(0)) {
                            var sr = outProc.StandardOutput.BaseStream;
                            var sw = inProc.StandardInput.BaseStream;

                            Thread.Sleep(2000);

                            while (sr.CanRead && !outProc.HasExited) {
                                sr.CopyTo(sw);
                                sw.Flush();
                            }
                            sw.Close();
                            return;
                        }
                    } catch (Exception e) {
                        throw e;
                    }
                })).Start();

                // 等待stdin端完成
                new Thread(new ThreadStart(() => {
                    inProcessor.waitForFinsih();
                    SetFinish();
                })).Start();

                new Thread(new ThreadStart(readStdErr)).Start();
                new Thread(new ThreadStart(readStdOut)).Start();
            } catch (Exception e) {
                throw e;
            }
        }

        public void stop()
        {
            if (!outProc.HasExited) {
                outProc.Kill();
            }

            if (!inProc.HasExited) {
                inProc.Kill();
            }

            pauseMre.Set();
            SetFinish();
        }

        public void waitForFinsih()
        {
            mre.WaitOne();
        }

        public void SetFinish()
        {
            mre.Set();
        }

        private void readStream(StreamReader sr, StreamType str)
        {
            string line;
            if (inProc != null) {
                try {
                    while ((line = sr.ReadLine()) != null) {
                        inProcessor.ProcessLine(line, str);
                    }
                } catch (Exception e) {
                    throw e;
                }
            }
        }

        private void readStdOut()
        {
            StreamReader sr = null;
            try {
                sr = inProc.StandardOutput;
            } catch (Exception e) {
                // log.LogValue("Exception getting IO reader for stdout", e, ImageType.Error);
                return;
            }
            readStream(sr, StreamType.Stdout);
        }

        private void readStdErr()
        {
            StreamReader sr = null;
            try {
                sr = inProc.StandardError;
            } catch (Exception e) {
                // log.LogValue("Exception getting IO reader for stderr", e, ImageType.Error);
                return;
            }
            readStream(sr, StreamType.Stderr);
        }
    }
}
