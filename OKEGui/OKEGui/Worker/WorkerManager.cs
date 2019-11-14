using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;


namespace OKEGui.Worker
{
    public enum WorkerType
    {
        Normal,
        Temporary,
    }

    public struct WorkerArgs
    {
        public string Name;
        public WorkerType RunningType;
        public TaskManager taskManager;
        public BackgroundWorker bgWorker;
        public int numaNode;
    }

    // 类似MeGUI的worker概念。每一个task（以及分离的Job）由worker来执行。多个Worker允许多开处理。
    // Worker执行Task的具体实现，见ExecuteTaskService里的WorkerDoWork()
    public partial class WorkerManager
    {
        public MainWindow MainWindow;
        public TaskManager tm;

        private List<string> workerList;
        private ConcurrentDictionary<string, WorkerType> workerType;

        // dummy object for locking.
        private object o = new object();

        private ConcurrentDictionary<string, BackgroundWorker> bgworkerlist;
        private int tempCounter;
        public bool IsRunning { get; protected set; }

        public delegate void Callback();

        public Callback AfterFinish = null;

        public WorkerManager(MainWindow mainWindow, TaskManager taskManager)
        {
            workerList = new List<string>();
            bgworkerlist = new ConcurrentDictionary<string, BackgroundWorker>();
            workerType = new ConcurrentDictionary<string, WorkerType>();
            MainWindow = mainWindow;
            tm = taskManager;
            IsRunning = false;
            tempCounter = 0;
        }

        public void AddTask(TaskDetail detail)
        {
            tm.AddTask(detail);
            if (IsRunning)
            {
                foreach (string worker in workerList)
                {
                    if (!bgworkerlist.ContainsKey(worker))
                    {
                        CreateWorker(worker);
                        StartWorker(worker);
                        break;
                    }
                }
            }
        }

        public bool Start()
        {
            lock (o)
            {
                if (workerList.Count == 0)
                {
                    return false;
                }

                int activeTaskCount = tm.GetActiveTaskCount();
                IsRunning = true;

                foreach (string worker in workerList)
                {
                    if (activeTaskCount == 0)
                    {
                        break;
                    }
                    if (!bgworkerlist.ContainsKey(worker))
                    {
                        CreateWorker(worker);
                        StartWorker(worker);
                        activeTaskCount--;
                    }
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
            if (!bgworkerlist.ContainsKey(name))
            {
                return false;
            }

            var worker = bgworkerlist[name];

            WorkerArgs args;
            args.Name = name;
            args.RunningType = workerType[name];
            args.taskManager = tm;
            args.bgWorker = worker;
            args.numaNode = NumaNode.NextNuma();
            Logger.Trace(name + "所拥有的Numa Node编号是" + args.numaNode.ToString());

            worker.RunWorkerAsync(args);
            return true;
        }

        public int GetWorkerCount()
        {
            lock (o)
            {
                return workerList.Count;
            }
        }

        public string AddTempWorker()
        {
            // 临时Worker只运行一次任务
            tempCounter++;
            string name = "Temp-" + tempCounter.ToString();

            lock (o)
            {
                workerList.Add(name);
                workerType.TryAdd(name, WorkerType.Temporary);
            }

            if (IsRunning)
            {
                CreateWorker(name);
                StartWorker(name);
            }

            return name;
        }

        public bool AddWorker(string name)
        {
            lock (o)
            {
                if (workerList.Contains(name))
                {
                    return false;
                }

                workerList.Add(name);
                workerType.TryAdd(name, WorkerType.Normal);
            }

            if (IsRunning)
            {
                CreateWorker(name);
                StartWorker(name);
            }

            return true;
        }

        public bool DeleteWorker(string name)
        {
            if (IsRunning)
            {
                return false;
            }

            lock (o)
            {
                bgworkerlist.TryRemove(name, out BackgroundWorker v);
                workerType.TryRemove(name, out WorkerType w);
                return workerList.Remove(name);
            }
        }

        public void StopWorker(string name)
        {
            // TODO
            IsRunning = false;

            if (bgworkerlist.ContainsKey(name))
            {
                if (bgworkerlist[name].IsBusy)
                {
                    bgworkerlist[name].CancelAsync();
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
