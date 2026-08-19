using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Threading;

namespace OKEGui
{
    // 线程安全Collection
    public class MTObservableCollection<T> : ObservableCollection<T>
    {
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;
            if (CollectionChanged != null)
                foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
                {
                    DispatcherObject dispObj = nh.Target as DispatcherObject;
                    if (dispObj != null)
                    {
                        Dispatcher dispatcher = dispObj.Dispatcher;
                        if (dispatcher != null && !dispatcher.CheckAccess())
                        {
                            dispatcher.BeginInvoke(
                                (Action)(() => nh.Invoke(this,
                                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
                                DispatcherPriority.DataBind);
                            continue;
                        }
                    }
                    nh.Invoke(this, e);
                }
        }
    }

    public class TaskManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("TaskManager");
        public MTObservableCollection<TaskDetail> taskStatus = new MTObservableCollection<TaskDetail>();

        private int tidCount = 0;

        private readonly HashSet<string> runningInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly object o = new object(); // dummy object used for locking threads.

        private static string NormalizePath(string path)
        {
            return new FileInfo(path).FullName;
        }

        private static string TaskIdentity(string configFilePath, string inputFile)
        {
            return NormalizePath(configFilePath) + "\0" + NormalizePath(inputFile);
        }

        public bool HasActiveTask(string configFilePath, string inputFile)
        {
            lock (o)
            {
                string identity = TaskIdentity(configFilePath, inputFile);
                foreach (TaskDetail task in taskStatus)
                {
                    if ((task.Progress == TaskStatus.TaskProgress.WAITING || task.Progress == TaskStatus.TaskProgress.RUNNING) &&
                        TaskIdentity(task.Taskfile.ConfigFilePath, task.InputFile) == identity)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int AddTask(TaskDetail detail)
        {
            lock (o)
            {
                TaskDetail td = detail;
                tidCount++;

                if (td.TaskName == "")
                {
                    td.TaskName = "新建任务 - " + tidCount.ToString();
                }

                // 初始化任务参数
                td.IsEnabled = true;
                td.Tid = tidCount.ToString();
                td.CurrentStatus = "等待中";
                td.Progress = TaskStatus.TaskProgress.WAITING;
                td.ProgressValue = 0.0;
                td.Speed = "0.0 fps";
                td.TimeRemain = TimeSpan.FromDays(30);
                td.WorkerName = "";

                taskStatus.Add(td);
                return taskStatus.Count;
            }
        }

        public bool DeleteTask(TaskDetail detail)
        {
            lock (o)
            {
                if (detail.Progress == TaskStatus.TaskProgress.RUNNING)
                {
                    return false;
                }
                else
                {
                    return taskStatus.Remove(detail);
                }
            }
        }

        private bool SwapTasksByIndex(int idx1, int idx2)
        {
            if (idx1 == idx2)
            {
                return false;
            }
            if (idx1 < 0 || idx1 >= taskStatus.Count || taskStatus[idx1].Progress != TaskStatus.TaskProgress.WAITING)
            {
                return false;
            }
            if (idx2 < 0 || idx2 >= taskStatus.Count || taskStatus[idx2].Progress != TaskStatus.TaskProgress.WAITING)
            {
                return false;
            }
            taskStatus.Move(idx1, idx2);
            return true;
        }

        public bool MoveTaskUp(TaskDetail td)
        {
            lock (o)
            {
                int idx1 = taskStatus.IndexOf(td);
                int idx2 = idx1 - 1;
                return SwapTasksByIndex(idx1, idx2);
            }
        }

        public bool MoveTaskDown(TaskDetail td)
        {
            lock (o)
            {
                int idx1 = taskStatus.IndexOf(td);
                int idx2 = idx1 + 1;
                return SwapTasksByIndex(idx1, idx2);
            }
        }

        public enum MoveTaskTopResult
        {
            OK, Already, Failure
        };

        public Enum MoveTaskTop(TaskDetail td)
        {
            lock(o)
            {
                int idx1 = taskStatus.IndexOf(td);
                int idIdleTask = 0;

                if (td.Progress != TaskStatus.TaskProgress.WAITING)
                {
                    return MoveTaskTopResult.Failure;
                }

                while (idIdleTask < taskStatus.Count && taskStatus[idIdleTask].Progress != TaskStatus.TaskProgress.WAITING)
                {
                    idIdleTask++;
                }

                if (idx1 == idIdleTask)
                {
                    return MoveTaskTopResult.Already;
                }

                taskStatus.Move(idx1, idIdleTask);
                return MoveTaskTopResult.OK;
            }
        }

        public TaskDetail GetNextTask()
        {
            lock (o)
            {
                foreach (var task in taskStatus)
                {
                    if (!task.IsEnabled || task.Progress != TaskStatus.TaskProgress.WAITING)
                    {
                        continue;
                    }

                    string inputKey = NormalizePath(task.InputFile);
                    if (runningInputs.Contains(inputKey))
                    {
                        continue;
                    }

                    runningInputs.Add(inputKey);
                    task.IsEnabled = false;
                    task.Progress = TaskStatus.TaskProgress.RUNNING;
                    return task;
                }
            }

            return null;
        }

        public void ReleaseInput(TaskDetail task)
        {
            if (task == null || string.IsNullOrEmpty(task.InputFile))
            {
                return;
            }

            lock (o)
            {
                runningInputs.Remove(NormalizePath(task.InputFile));
            }
        }

        public bool HasNextTask()
        {
            lock (o)
            {
                // 找出下一个可用任务
                foreach (var task in taskStatus)
                {
                    if (task.IsEnabled && task.Progress == TaskStatus.TaskProgress.WAITING)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int GetActiveTaskCount()
        {
            lock (o)
            {
                int activeTaskCount = 0;

                foreach (var task in taskStatus)
                {
                    if (task.IsEnabled && task.Progress == TaskStatus.TaskProgress.WAITING)
                    {
                        activeTaskCount++;
                    }
                }

                return activeTaskCount;
            }
        }

        public List<TaskDetail> GetNotRunningTasks()
        {
            lock (o)
            {
                List<TaskDetail> notRunningTasks = new List<TaskDetail>();

                foreach (var task in taskStatus)
                {
                    if (task.IsEnabled && task.Progress != TaskStatus.TaskProgress.RUNNING)
                    {
                        notRunningTasks.Add(task);
                    }
                }

                return notRunningTasks;
            }
        }

        public List<TaskDetail> GetRunningTasks()
        {
            lock (o)
            {
                List<TaskDetail> runningTasks = new List<TaskDetail>();

                foreach (var task in taskStatus)
                {
                    if (task.Progress == TaskStatus.TaskProgress.RUNNING)
                    {
                        runningTasks.Add(task);
                    }
                }

                return runningTasks;
            }
        }

        public int GetEnabledTaskCount()
        {
            lock (o)
            {
                int enabledTaskCount = 0;

                foreach (var task in taskStatus)
                {
                    if (task.IsEnabled)
                    {
                        enabledTaskCount++;
                    }
                }

                return enabledTaskCount;
            }
        }

        public int GetTaskCount()
        {
            lock (o)
            {
                return taskStatus.Count;
            }
        }

        public void UpdateChapterStatus()
        {
            lock (o)
            {
                // 找出下一个可用任务
                foreach (TaskDetail task in taskStatus)
                {
                    if (task.IsEnabled && task.Progress == TaskStatus.TaskProgress.WAITING)
                    {
                        task.ChapterStatus = ChapterService.UpdateChapterStatus(task);
                    }
                }
            }
        }

        public List<TaskDetail> GetTasksByInputFile(string inputFile)
        {
            lock (o)
            {
                List<TaskDetail> res = new List<TaskDetail>();
                foreach (TaskDetail i in taskStatus)
                {
                    if (i.InputFile == inputFile)
                    {
                        res.Add(i);
                    }
                }
                return res;
            }
        }

        public bool AllSuccess()
        {
            lock (o)
            {
                if (taskStatus.Count == 0)
                {
                    return false;
                }

                foreach (TaskDetail i in taskStatus)
                {
                    if (i.Progress != TaskStatus.TaskProgress.FINISHED)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
