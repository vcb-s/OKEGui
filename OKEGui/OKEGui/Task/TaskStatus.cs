using System;
using System.ComponentModel;
using static OKEGui.RpChecker;

namespace OKEGui
{
    /// <summary>
    /// 继承自INotifyPropertyChanged接口来实现数据的实时显示
    /// 每一个域都与MainWindow里的显示挂钩。
    /// </summary>
    public class TaskStatus : INotifyPropertyChanged
    {
        /// <summary>
        /// 任务是否启用
        /// </summary>
        private bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsEnabled"));
            }
        }

        /// <summary>
        /// 任务名称
        /// </summary>
        private string taskName;
        public string TaskName
        {
            get { return taskName; }
            set
            {
                taskName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TaskName"));
            }
        }

        /// <summary>
        /// 输入文件
        /// </summary>
        private string inputFile;
        public string InputFile
        {
            get { return inputFile; }
            set
            {
                inputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("InputFile"));
            }
        }

        /// <summary>
        /// 章节信息
        /// </summary>
        private ChapterStatus chapterStatus;
        public ChapterStatus ChapterStatus
        {
            get => chapterStatus;
            set
            {
                chapterStatus = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ChapterStatus"));
            }
        }

        /// <summary>
        /// 输出文件
        /// </summary>
        private string outputFile;
        public string OutputFile
        {
            get { return outputFile; }
            set
            {
                outputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OutputFile"));
            }
        }

        /// <summary>
        /// 任务总体进度
        /// </summary>

        public enum TaskProgress : int { WAITING = 0, RUNNING, ERROR, FINISHED }
        public TaskProgress Progress;


        /// <summary>
        /// 任务执行状态
        /// </summary>
        private string currentStatus;
        public string CurrentStatus
        {
            get { return currentStatus; }
            set
            {
                currentStatus = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentStatus"));
            }
        }

        /// <summary>
        /// 当前进度（子任务进度）
        /// </summary>
        private double progressValue;
        public double ProgressValue
        {
            get { return progressValue; }
            set
            {
                progressValue = value;
                // Indetermate
                if (progressValue < 0)
                {
                    ProgressStr = "";
                    IsUnKnowProgress = true;
                }
                else
                {
                    ProgressStr = progressValue.ToString("0.00") + "%";
                    IsUnKnowProgress = false;
                }

                OnPropertyChanged(new PropertyChangedEventArgs("ProgressValue"));
            }
        }

        private string progressStr;
        public string ProgressStr
        {
            get { return progressStr; }
            set
            {
                progressStr = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ProgressStr"));
            }
        }

        /// <summary>
        /// 任务进度状态（进度未知）
        /// </summary>
        private bool isUnKnowProgress;
        public bool IsUnKnowProgress
        {
            get { return isUnKnowProgress; }
            set
            {
                isUnKnowProgress = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsUnKnowProgress"));
            }
        }

        /// <summary>
        /// 任务速度
        /// </summary>
        private string speed;
        public string Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Speed"));
            }
        }

        /// <summary>
        /// 码率
        /// </summary>
        private string bitrate;
        public string BitRate
        {
            get { return bitrate; }
            set
            {
                bitrate = value;
                OnPropertyChanged(new PropertyChangedEventArgs("BitRate"));
            }
        }

        /// <summary>
        /// 剩余时间
        /// </summary>
        private TimeSpan timeRemain;
        public TimeSpan TimeRemain
        {
            get { return timeRemain; }
            set
            {
                timeRemain = value;
                TimeRemainStr = value.ToString(@"hh\:mm\:ss");
                if (value.TotalHours > 24.0*7)
                {
                    TimeRemainStr = "大于一周";
                }
            }
        }

        private string timeRemainStr;
        public string TimeRemainStr
        {
            get { return timeRemainStr; }
            set
            {
                timeRemainStr = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TimeRemainStr"));
            }
        }

        /// <summary>
        /// 正在执行的工作单元名称
        /// </summary>
        private string workerName;
        public string WorkerName
        {
            get { return workerName; }
            set
            {
                workerName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("WorkerName"));
            }
        }

        /// <summary>
        /// 花屏检测信息
        /// </summary>
        private RpcStatus rpcStatus;
        public string RpcStatus
        {
            get { return rpcStatus.ToString(); }
            set
            {
                rpcStatus = (RpcStatus)Enum.Parse(typeof(RpcStatus), value);
                OnPropertyChanged(new PropertyChangedEventArgs("RpcStatus"));
                OnPropertyChanged(new PropertyChangedEventArgs("RpcButtonEnabled"));
            }
        }

        private string rpcOutput;
        public string RpcOutput
        {
            get { return rpcOutput; }
            set
            {
                rpcOutput = value;
                OnPropertyChanged(new PropertyChangedEventArgs("RpcOutput"));
            }
        }

        public bool RpcButtonEnabled
        {
            get { return rpcStatus == RpChecker.RpcStatus.未通过 || rpcStatus == RpChecker.RpcStatus.通过; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
