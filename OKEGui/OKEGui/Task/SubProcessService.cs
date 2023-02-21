using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui.Task
{
    public class SubProcessService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("SubProcessService");

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        private static void SuspendProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        private static void ResumeProcess(Process process)
        {
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                int suspendCount = 2;
                while (suspendCount > 1)
                {
                    suspendCount = ResumeThread(pOpenThread);
                }

                CloseHandle(pOpenThread);
            }
        }

        private static List<Process> getChildProcesses(Process process)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT *" +
                " FROM Win32_Process" +
                " WHERE ParentProcessId=" + process.Id.ToString());
            ManagementObjectCollection collection = searcher.Get();
            List<Process> res = new List<Process>();
            if (collection.Count > 0)
            {
                foreach (ManagementBaseObject item in collection)
                {
                    UInt32 childProcessId = (UInt32)item["ProcessId"];
                    if ((int)childProcessId != Process.GetCurrentProcess().Id)
                    {
                        Process childProcess = Process.GetProcessById((int)childProcessId);
                        res.Add(childProcess);
                        res.AddRange(getChildProcesses(childProcess));
                    }
                }
            }
            return res;
        }

        public static void PauseAll()
        {
            List<Process> allProcesses = getChildProcesses(Process.GetCurrentProcess());
            foreach (Process i in allProcesses)
            {
                SuspendProcess(i);
            }
        }

        public static void ResumeAll()
        {
            List<Process> allProcesses = getChildProcesses(Process.GetCurrentProcess());
            foreach (Process i in allProcesses)
            {
                ResumeProcess(i);
            }
        }

        public static void KillAll()
        {
            List<Process> allProcesses = getChildProcesses(Process.GetCurrentProcess());
            foreach (Process i in allProcesses)
            {
                i.Kill();
            }
        }
    }
}
