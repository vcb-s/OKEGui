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

    public class Worker
    {
        public int Wid;
        public string Name;
        public WorkerType WType;
    }

    public struct WorkerArgs
    {
        public int Wid;
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

        private ConcurrentDictionary<string, Worker> workerList;

        // dummy object for locking.
        private object o = new object();

        private ConcurrentDictionary<string, BackgroundWorker> bgworkerlist;
        private int tempCounter;
        public bool IsRunning { get; protected set; }

        public delegate void Callback(MainWindow window);

        public Callback AfterFinish = null;

        public WorkerManager(MainWindow mainWindow, TaskManager taskManager)
        {
            workerList = new ConcurrentDictionary<string, Worker>();
            bgworkerlist = new ConcurrentDictionary<string, BackgroundWorker>();
            MainWindow = mainWindow;
            tm = taskManager;
            IsRunning = false;
            tempCounter = 0;
        }

        public void AddTask(TaskDetail detail)
        {
            tm.AddTask(detail);
            TryStartNewWorker();
        }

        public bool Start()
        {
            lock (o)
            {
                if (workerList.Count == 0)
                {
                    return false;
                }

                IsRunning = true;
                TryStartNewWorker();

                return true;
            }
        }

        public void Stop()
        {
            lock (o)
            {
                StopAllWorker();
                IsRunning = false;
            }
        }

        public bool TryStartNewWorker()
        {
            if (!IsRunning)
            {
                return false;
            }

            int activeTaskCount = tm.GetActiveTaskCount();
            bool startNew = false;

            foreach (var worker in workerList)
            {
                if (activeTaskCount == 0)
                {
                    break;
                }
                if (!bgworkerlist.ContainsKey(worker.Value.Name))
                {
                    CreateWorker(worker.Value.Name);
                    StartWorker(worker.Value);
                    activeTaskCount--;
                    startNew = true;
                }
            }

            return startNew;
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

        public bool StartWorker(Worker worker)
        {
            if (!bgworkerlist.ContainsKey(worker.Name))
            {
                return false;
            }

            var bgWorker = bgworkerlist[worker.Name];

            WorkerArgs args;
            args.Wid = worker.Wid;
            args.Name = worker.Name;
            args.RunningType = worker.WType;
            args.taskManager = tm;
            args.bgWorker = bgWorker;
            args.numaNode = NumaNode.NextNuma();
            Logger.Trace(args.Name + "所拥有的Numa Node编号是" + args.numaNode.ToString());

            bgWorker.RunWorkerAsync(args);
            return true;
        }

        public int GetWorkerCount()
        {
            lock (o)
            {
                return workerList.Count;
            }
        }

        public int GetBGWorkerCount()
        {
            lock (o)
            {
                return bgworkerlist.Count;
            }
        }

        public string AddTempWorker()
        {
            // 临时Worker只运行一次任务
            tempCounter++;
            string name = "Temp-" + tempCounter.ToString();
            Worker tmpWorker = new Worker{
                Wid = tempCounter,
                Name = name,
                WType = WorkerType.Temporary
            };

            lock (o)
            {
                workerList.TryAdd(name, tmpWorker);
            }

            if (IsRunning)
            {
                CreateWorker(name);
                StartWorker(tmpWorker);
            }

            return name;
        }

        public bool AddWorker(int Wid)
        {
            Worker worker = new Worker{
                Wid = Wid,
                Name = $"工作单元-{Wid}",
                WType = WorkerType.Normal
            };

            lock (o)
            {
                if (workerList.ContainsKey(Wid.ToString()))
                {
                    return false;
                }
                workerList.TryAdd(Wid.ToString(), worker);
            }

            TryStartNewWorker();

            return true;
        }

        public bool DeleteWorker(string name)
        {
            lock (o)
            {
                if (!workerList.ContainsKey(name))
                {
                    return true;
                }
                else
                {
                    return workerList.TryRemove(name, out Worker w);
                }
            }
        }

        public void StopWorker(string name)
        {
            lock (o)
            {
                if (bgworkerlist.ContainsKey(name))
                {
                    bgworkerlist[name].CancelAsync();
                    bgworkerlist[name].Dispose();
                    bgworkerlist.TryRemove(name, out BackgroundWorker v);
                    Logger.Debug($"已终止{name}");
                }
            }
        }

        public void StopAllWorker()
        {
            lock (o)
            {
                foreach (var w in bgworkerlist)
                {
                    StopWorker(w.Key);
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
