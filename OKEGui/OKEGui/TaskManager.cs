using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        //List<update> updates = new List<update>();
        //List<T>不支持添加 删除数据时UI界面的响应,所以改用ObservableCollection<T>
        public MTObservableCollection<TaskDetail> taskStatus = new MTObservableCollection<TaskDetail>();

        private int newTaskCount = 1;
        private int tidCount = 0;

        public bool isCanStart = false;
        private object o = new object();

        public bool CheckTask(TaskDetail td)
        {
            if (td.InputScript == "") {
                return false;
            }

            if (td.InputFile == "") {
                return false;
            }

            if (td.EncoderPath == "") {
                return false;
            }

            if (td.EncoderParam == "") {
                // 这里只是额外参数，必要参数会在执行任务之前加上
            }

            if (td.OutputFile == "") {
                return false;
            }

            if (td.VideoFormat == "") {
                return false;
            }

            if (td.AudioFormat == "") {
                return false;
            }

            return true;
        }

        // 新建空白任务
        public int AddTask()
        {
            taskStatus.Add(new TaskDetail(false, (taskStatus.Count + 1).ToString(), "新建任务 - " + newTaskCount.ToString(),
                "", "", "需要修改", 0.0, "0.0 fps", TimeSpan.FromDays(30)));

            newTaskCount = newTaskCount + 1;

            return taskStatus.Count;
        }

        public int AddTask(TaskDetail detail)
        {
            TaskDetail td = detail;

            if (td.TaskName == "") {
                td.TaskName = "新建任务 - " + newTaskCount.ToString();
                newTaskCount = newTaskCount + 1;
            }

            if (!CheckTask(td)) {
                return -1;
            }

            tidCount++;

            // 初始化任务参数
            td.IsEnabled = true;                            // 默认启用
            td.Tid = tidCount.ToString();
            // td.TaskName = detail.TaskName;
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
            return this.DeleteTask(detail.Tid);
        }

        public bool DeleteTask(string tid)
        {
            if (Int32.Parse(tid) < 1) {
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

        public bool UpdateTask(TaskDetail detail)
        {
            if (!CheckTask(detail)) {
                return false;
            }

            if (Int32.Parse(detail.Tid) < 1) {
                return false;
            }

            taskStatus[Int32.Parse(detail.Tid) - 1] = detail;

            return true;
        }

        public TaskDetail GetNextTask()
        {
            if (!isCanStart) {
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
    }
}
