using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OKEGui
{
    public enum StreamType : ushort { None = 0, Stderr = 1, Stdout = 2 }

    public abstract class CommandlineJobProcessor/*<TJob>*/ : IJobProcessor
    //where TJob : Job
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("CommandlineJobProcessor");
        #region variables

        // protected Job job;

        protected DateTime startTime;
        protected bool isProcessing = false;
        protected Process proc = new Process(); // the encoder process
        protected string executable; // path and filename of the commandline encoder to be used
        protected string commandLine;
        protected ManualResetEvent mre = new ManualResetEvent(true); // lock used to pause encoding
        protected ManualResetEvent finishMre = new ManualResetEvent(false);
        protected ManualResetEvent stdoutDone = new ManualResetEvent(false);
        protected ManualResetEvent stderrDone = new ManualResetEvent(false);
        protected StatusUpdate su;
        protected Thread readFromStdErrThread;
        protected Thread readFromStdOutThread;
        protected List<string> tempFiles = new List<string>();
        protected bool bRunSecondTime = false;
        protected bool bWaitForExit = false;

        //protected LogItem log;
        //protected LogItem stdoutLog;
        //protected LogItem stderrLog;

        #endregion variables

        // returns true if the exit code yields a meaningful answer
        protected virtual bool checkExitCode
        {
            get { return true; }
        }

        protected virtual void getErrorLine()
        {
            return;
        }

        private void proc_Exited2(object sender, EventArgs e)
        {
            Process p = sender as Process;
            Logger.Debug("exitCode={}", p.ExitCode);
            onExited(p.ExitCode);
        }


        protected virtual void onExited(int exitCode)
        {
            return;
        }

        /// <summary>
        /// handles the encoder process existing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void proc_Exited(object sender, EventArgs e)
        {
            mre.Set();  // Make sure nothing is waiting for pause to stop
            stdoutDone.WaitOne(); // wait for stdout to finish processing
            stderrDone.WaitOne(); // wait for stderr to finish processing

            // check the exitcode
            if (checkExitCode && proc.ExitCode != 0) {
                getErrorLine();
                string strError = WindowUtil.GetErrorText(proc.ExitCode);
                if (!su.WasAborted) {
                    su.HasError = true;
                    // log.LogEvent("Process exits with error: " + strError, ImageType.Error);
                } else {
                    // log.LogEvent("Process exits with error: " + strError);
                }
            }

            if (bRunSecondTime) {
                bRunSecondTime = false;
                start();
            } else {
                su.IsComplete = true;
                StatusUpdate(su);
            }

            bWaitForExit = false;
        }

        #region IVideoEncoder overridden Members

        // TODO: 默认优先级
        public void start()
        {
            proc = new Process();
            ProcessStartInfo pstart = new ProcessStartInfo();
            pstart.FileName = executable;
            pstart.Arguments = commandLine;
            pstart.RedirectStandardOutput = true;
            pstart.RedirectStandardError = true;
            pstart.WindowStyle = ProcessWindowStyle.Minimized;
            pstart.CreateNoWindow = true;
            pstart.UseShellExecute = false;
            proc.StartInfo = pstart;
            proc.EnableRaisingEvents = true;
            //proc.Exited += new EventHandler(proc_Exited);
            proc.Exited += new EventHandler(proc_Exited2);
            bWaitForExit = false;
            Logger.Info(pstart.FileName + " " + pstart.Arguments);

            try {
                bool started = proc.Start();
                // startTime = DateTime.Now;
                isProcessing = true;
                Logger.Debug(executable + "开始运行");
                proc.PriorityClass = ProcessPriorityClass.BelowNormal;
                readStdErr();
                readStdOut();
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
                    su.WasAborted = true;
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

        #endregion IVideoEncoder overridden Members

        #region reading process output

        public virtual string Executable
        {
            get { return executable; }
        }

        protected virtual void readStream(StreamReader sr, ManualResetEvent rEvent, StreamType str)
        {
            string line;
            if (proc != null)
            {
                while ((line = sr.ReadLine()) != null)
                {
                    mre.WaitOne();

                    Debugger.Log(0, "readstream", str.ToString() + line + "\n");
                    ProcessLine(line, str);
                }
                rEvent.Set();
            }
        }

        protected void readStdOut()
        {
            StreamReader sr = null;
            try {
                sr = proc.StandardOutput;
            } catch (Exception e) {
                Debugger.Log(0, "", "Exception getting IO reader for stdout" + e.ToString());
                stdoutDone.Set();
                return;
            }
            readStream(sr, stdoutDone, StreamType.Stdout);
        }

        protected void readStdErr()
        {
            StreamReader sr = null;
            try {
                sr = proc.StandardError;
            } catch (Exception e) {
                Debugger.Log(0, "", "Exception getting IO reader for stderr" + e.ToString());
                stderrDone.Set();
                return;
            }
            readStream(sr, stderrDone, StreamType.Stderr);
        }

        public virtual void ProcessLine(string line, StreamType stream)
        {
            Logger.Trace(line);
        }

        #endregion reading process output

        #region status updates

        public event JobProcessingStatusUpdateCallback StatusUpdate;

        protected void RunStatusCycle()
        {
            while (isRunning()) {
                su.TimeElapsed = DateTime.Now - startTime;
                // su.CurrentFileSize = (ulong)new FileInfo(job.Output).Length;

                doStatusCycleOverrides();
                su.FillValues();
                if (StatusUpdate != null && proc != null && !proc.HasExited)
                    StatusUpdate(su);

                Thread.Sleep(1000);
            }
        }

        protected virtual void doStatusCycleOverrides()
        { }

        #endregion status updates
    }
}
