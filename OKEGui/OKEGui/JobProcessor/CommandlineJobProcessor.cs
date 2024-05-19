using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;

namespace OKEGui.JobProcessor
{
    public enum StreamType : ushort { None = 0, Stderr = 1, Stdout = 2 }

    public abstract class CommandlineJobProcessor : IJobProcessor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("CommandlineJobProcessor");
        #region variables

        protected Job job;
        protected DateTime startTime;
        protected bool isProcessing = false;
        protected Process proc = new Process(); // the encoder process
        protected string executable; // path and filename of the commandline encoder to be used
        protected string commandLine;
        protected ManualResetEvent mre = new ManualResetEvent(true); // lock used to pause encoding
        protected ManualResetEvent finishMre = new ManualResetEvent(false);
        protected List<string> tempFiles = new List<string>();
        protected bool bWaitForExit = false;

        #endregion variables

        public CommandlineJobProcessor(Job job)
        {
            this.job = job;
        }

        // returns true if the exit code yields a meaningful answer
        protected virtual bool checkExitCode
        {
            get { return true; }
        }

        protected virtual void getErrorLine()
        {
            return;
        }

        private void proc_Exited(object sender, EventArgs e)
        {
            mre.Set();  // Make sure nothing is waiting for pause to stop
            Process p = sender as Process;
            Logger.Debug("exitCode={}", p.ExitCode);
            onExited(p.ExitCode);
        }

        protected virtual void onExited(int exitCode)
        {
            return;
        }

        #region IJobProcessor overridden Members

        // TODO: 默认优先级
        public void start()
        {
            proc = new Process();
            ProcessStartInfo pstart = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = commandLine,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WindowStyle = ProcessWindowStyle.Minimized,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            proc.StartInfo = pstart;
            proc.EnableRaisingEvents = true;
            proc.Exited += new EventHandler(proc_Exited);
            proc.OutputDataReceived += ((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    mre.WaitOne();
                    ProcessLine(e.Data, StreamType.Stdout);
                }
            });
            proc.ErrorDataReceived += ((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    mre.WaitOne();
                    ProcessLine(e.Data, StreamType.Stderr);
                }
            });
            bWaitForExit = false;
            Logger.Info(pstart.FileName + " " + pstart.Arguments);

            try {
                proc.Start();
                startTime = DateTime.Now;
                isProcessing = true;
                proc.PriorityClass = ProcessPriorityClass.BelowNormal;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                Logger.Debug(executable + "开始运行");
            } catch (Exception e) {
                Logger.Error(e.StackTrace);
                throw e;
            }
        }

        public void stop()
        {
            if (proc != null && !proc.HasExited) {
                try {
                    bWaitForExit = true;
                    mre.Set(); // if it's paused, then unpause
                    proc.Kill();
                    while (bWaitForExit) // wait until the process has terminated without locking the GUI
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(100);
                    }
                    proc.WaitForExit();
                    return;
                } catch (Exception e) {
                    throw e;
                }
            } else {
                if (proc == null)
                    throw new Exception("Encoder process does not exist");
                else
                    throw new Exception("Encoder process has already existed");
            }
        }

        public void pause()
        {
            if (!canPause)
                throw new Exception("Can't pause this kind of job.");
            if (!mre.Reset())
                throw new Exception("Could not reset mutex. pause failed");
        }

        public void resume()
        {
            if (!canPause)
                throw new Exception("Can't resume this kind of job.");
            if (!mre.Set())
                throw new Exception("Could not set mutex. pause failed");
        }

        public virtual void waitForFinish()
        {
            proc.WaitForExit();
            finishMre.WaitOne();
        }

        public void SetFinish()
        {
            Logger.Debug("结束运行 " + executable);
            finishMre.Set();
        }

        public bool isRunning()
        {
            return (proc != null && !proc.HasExited);
        }

        public void changePriority(ProcessPriority priority)
        {
            if (isRunning()) {
                try {
                    switch (priority) {
                        case ProcessPriority.IDLE:
                            proc.PriorityClass = ProcessPriorityClass.Idle;
                            break;

                        case ProcessPriority.BELOW_NORMAL:
                            proc.PriorityClass = ProcessPriorityClass.BelowNormal;
                            break;

                        case ProcessPriority.NORMAL:
                            proc.PriorityClass = ProcessPriorityClass.Normal;
                            break;

                        case ProcessPriority.ABOVE_NORMAL:
                            proc.PriorityClass = ProcessPriorityClass.AboveNormal;
                            break;

                        case ProcessPriority.HIGH:
                            proc.PriorityClass = ProcessPriorityClass.RealTime;
                            break;
                    }
                    VistaStuff.SetProcessPriority(proc.Handle, proc.PriorityClass);
                    return;
                } catch (Exception e) // process could not be running anymore
                  {
                    throw e;
                }
            } else {
                if (proc == null)
                    throw new Exception("Process has not been started yet");
                else {
                    Debug.Assert(proc.HasExited);
                    throw new Exception("Process has exited");
                }
            }
        }

        public virtual bool canPause
        {
            get { return true; }
        }

        #endregion IJobProcessor overridden Members

        #region reading process output

        public virtual string Executable
        {
            get { return executable; }
        }

        public virtual void ProcessLine(string line, StreamType stream)
        {
            Logger.Trace(line);
        }

        #endregion reading process output
    }
}
