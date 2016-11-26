using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace OKEGui
{
    // TODO: 目前只考虑压制全部任务；以后可能会各步骤分开进行，或者进行其他任务
    // TODO: TaskManger 做成接口。各种不同类型任务分开管理。
    public enum WorkerType
    {
        Normal,
        Temporary,
    }

    struct WorkerArgs
    {
        public string Name;
        public WorkerType RunningType;
        public TaskManager taskManager;
        public BackgroundWorker bgWorker;
    }

    public class WorkerManager
    {
        public TaskManager tm;

        private List<string> workerList;

        private object o = new object();

        private ConcurrentDictionary<string, BackgroundWorker> bgworkerlist;
        private ConcurrentDictionary<string, WorkerType> workerType;
        private int tempCounter;
        private bool isRunning;

        public delegate void Callback();

        public Callback AfterFinish = null;

        public WorkerManager(TaskManager taskManager)
        {
            workerList = new List<string>();
            bgworkerlist = new ConcurrentDictionary<string, BackgroundWorker>();
            workerType = new ConcurrentDictionary<string, WorkerType>();
            tm = taskManager;
            isRunning = false;
            tempCounter = 0;
        }

        public bool Start()
        {
            lock (o) {
                if (workerList.Count == 0) {
                    return false;
                }

                isRunning = true;

                foreach (string worker in workerList) {
                    if (bgworkerlist.ContainsKey(worker)) {
                        BackgroundWorker bg;
                        bgworkerlist.TryRemove(worker, out bg);
                    }

                    CreateWorker(worker);
                    StartWorker(worker);
                }

                return true;
            }
        }

        public bool CreateWorker(string name)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(WorkerDoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(WorkerProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerCompleted);

            return bgworkerlist.TryAdd(name, worker);
        }

        public bool StartWorker(string name)
        {
            if (!bgworkerlist.ContainsKey(name)) {
                return false;
            }

            var worker = bgworkerlist[name];

            WorkerArgs args;
            args.Name = name;
            args.RunningType = workerType[name];
            args.taskManager = tm;
            args.bgWorker = worker;

            worker.RunWorkerAsync(args);

            return true;
        }

        public int GetWorkerCount()
        {
            lock (o) {
                return workerList.Count;
            }
        }

        public string AddTempWorker()
        {
            // 临时Worker只运行一次任务
            tempCounter++;
            string name = "Temp-" + tempCounter.ToString();

            lock (o) {
                workerList.Add(name);
                Debug.Assert(workerType.TryAdd(name, WorkerType.Temporary));
            }

            if (isRunning) {
                CreateWorker(name);
                StartWorker(name);
            }

            return name;
        }

        public bool AddWorker(string name)
        {
            lock (o) {
                if (workerList.Contains(name)) {
                    return false;
                }

                workerList.Add(name);
                Debug.Assert(workerType.TryAdd(name, WorkerType.Normal));
            }

            if (isRunning) {
                CreateWorker(name);
                StartWorker(name);
            }

            return true;
        }

        public bool DeleteWorker(string name)
        {
            if (isRunning) {
                return false;
            }

            lock (o) {
                BackgroundWorker v;
                bgworkerlist.TryRemove(name, out v);

                WorkerType w;
                workerType.TryRemove(name, out w);

                return workerList.Remove(name);
            }
        }

        public void StopWorker(string name)
        {
            // TODO
            isRunning = false;

            if (bgworkerlist[name].IsBusy) {
                bgworkerlist[name].CancelAsync();
            }
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (WorkerArgs)e.Argument;

            while (isRunning) {
                Job j = args.taskManager.GetNextJob();

                if (j == null) {
                    // 全部工作完成
                    lock (o) {
                        BackgroundWorker v;
                        bgworkerlist.TryRemove(args.Name, out v);

                        WorkerType w;
                        workerType.TryRemove(args.Name, out w);

                        if (bgworkerlist.Count == 0 && workerType.Count == 0) {
                            if (AfterFinish != null) {
                                AfterFinish();
                            }
                        }
                    }
                    return;
                }

                JobWorker worker = new JobWorker(j);
                (j as VideoJob).config.WorkerName = args.Name;
                (j as VideoJob).config.IsEnabled = false;

                // TODO: 计时
                worker.Start();

                (j as VideoJob).config.WorkerName = "";

                if (args.RunningType == WorkerType.Temporary) {
                    DeleteWorker(args.Name);
                    return;
                }
            }
        }

        private void WorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}
