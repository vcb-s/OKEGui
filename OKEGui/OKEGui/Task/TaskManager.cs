using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public MTObservableCollection<TaskDetail> taskStatus = new MTObservableCollection<TaskDetail>();

        private int tidCount = 0;

        private readonly object o = new object(); // dummy object used for locking threads.

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
                // 找出下一个可用任务
                foreach (var task in taskStatus)
                {
                    if (task.IsEnabled)
                    {
                        task.IsEnabled = false;
                        return task;
                    }
                }
            }

            return null;
        }

        public bool HasNextTask()
        {
            lock (o)
            {
                // 找出下一个可用任务
                foreach (var task in taskStatus)
                {
                    if (task.IsEnabled)
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
                    if (task.IsEnabled)
                    {
                        activeTaskCount++;
                    }
                }

                return activeTaskCount;
            }
        }

        public void UpdateChapterStatus()
        {
            lock (o)
            {
                // 找出下一个可用任务
                foreach (TaskDetail task in taskStatus)
                {
                    if (task.IsEnabled)
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
