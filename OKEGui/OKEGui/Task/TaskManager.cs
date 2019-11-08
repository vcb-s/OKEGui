using System;
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
                foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList()) {
                    DispatcherObject dispObj = nh.Target as DispatcherObject;
                    if (dispObj != null) {
                        Dispatcher dispatcher = dispObj.Dispatcher;
                        if (dispatcher != null && !dispatcher.CheckAccess()) {
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
        public MTObservableCollection<TaskDetail> taskStatus = new MTObservableCollection<TaskDetail>();

        private int newTaskCount = 0;
        private int tidCount = 0;

        public bool IsCanStart = false;
        private readonly object o = new object(); // dummy object used for locking threads.

        public int AddTask(TaskDetail detail)
        {
            TaskDetail td = detail;
            newTaskCount++;
            tidCount++;

            if (td.TaskName == "") {
                td.TaskName = "新建任务 - " + newTaskCount.ToString();
            }

            // 初始化任务参数
            td.IsEnabled = true;
            td.Tid = tidCount.ToString();
            td.CurrentStatus = "等待中";
            td.ProgressValue = 0.0;
            td.Speed = "0.0 fps";
            td.TimeRemain = TimeSpan.FromDays(30);
            td.WorkerName = "";

            taskStatus.Add(td);
            return taskStatus.Count;
        }

        public bool DeleteTask(TaskDetail detail)
        {
            return DeleteTask(detail.Tid);
        }

        public bool DeleteTask(string tid)
        {
            if (int.Parse(tid) < 1) {
                return false;
            }

            try {
                foreach (var item in taskStatus) {
                    if (item.Tid == tid) {
                        if (item.IsRunning) {
                            return false;
                        }
                        taskStatus.Remove(item);
                        return true;
                    }
                }
            } catch (ArgumentOutOfRangeException) {
                return false;
            }

            return false;
        }

        public TaskDetail GetNextTask()
        {
            if (!IsCanStart) {
                return null;
            }

            lock (o) {
                // 找出下一个可用任务
                foreach (var task in taskStatus) {
                    if (task.IsEnabled) {
                        task.IsEnabled = false;
                        task.IsRunning = true;
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
                        activeTaskCount ++;
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

        public bool HasInputFile(string inputFile)
        {
            foreach (TaskDetail i in taskStatus)
            {
                if (i.InputFile == inputFile)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
