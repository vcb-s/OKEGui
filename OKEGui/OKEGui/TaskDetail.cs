using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace OKEGui
{
    //继承自INotifyPropertyChanged接口来实现数据的实时显示
    public class TaskStatus : INotifyPropertyChanged
    {
        /// <summary>
        /// 任务是否启用
        /// </summary>
        private bool isEnabled;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set {
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
            set {
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
            set {
                inputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("InputFile"));
            }
        }

        /// <summary>
        /// 输出文件
        /// </summary>
        private string outputFile;

        public string OutputFile
        {
            get { return outputFile; }
            set {
                outputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OutputFile"));
            }
        }

        /// <summary>
        /// 任务执行状态
        /// </summary>
        private string currentStatus;

        public string CurrentStatus
        {
            get { return currentStatus; }
            set {
                currentStatus = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentStatus"));
            }
        }

        /// <summary>
        /// 当前进度（子任务进度）
        /// </summary>
        private double progress;

        public double ProgressValue
        {
            get { return progress; }
            set {
                progress = value;
                // Indetermate
                if (progress < 0)
                {
                    ProgressStr = "";
                    IsUnKnowProgress = true;
                }
                else
                {
                    ProgressStr = progress.ToString("0.00") + "%";
                    IsUnKnowProgress = false;
                }

                OnPropertyChanged(new PropertyChangedEventArgs("ProgressValue"));
            }
        }

        private string progressStr;

        public string ProgressStr
        {
            get { return progressStr; }
            set {
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
            set {
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
            set {
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
            set {
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
            set {
                timeRemain = value;
                TimeRemainStr = value.ToString(@"hh\:mm\:ss");
                if (value.TotalHours > 24.0)
                {
                    TimeRemainStr = "大于一天";
                }
            }
        }

        private string timeRemainStr;

        public string TimeRemainStr
        {
            get { return timeRemainStr; }
            set {
                timeRemainStr = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TimeRemainStr"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
    }

    public class TaskDetail : TaskStatus
    {
        // 工作队列
        public Queue<Job> JobQueue = new Queue<Job>();

        #region JobConfig

        /// <summary>
        /// 是否在运行
        /// </summary>
        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set {
                isRunning = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsRunning"));
            }
        }

        /// <summary>
        /// 任务ID
        /// </summary>
        private string tid;

        public string Tid
        {
            get { return tid; }
            set {
                tid = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Tid"));
            }
        }

        /// <summary>
        /// 正在执行的工作单元名称
        /// </summary>
        private string workerName;

        public string WorkerName
        {
            get { return workerName; }
            set {
                workerName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("WorkerName"));
            }
        }

        /// <summary>
        /// 输出文件结构
        /// </summary>
        public MediaFile MediaOutFile = new MediaFile();

        /// <summary>
        /// 封装容器格式
        /// </summary>
        /// <remarks>e.g. mp4, mkv, none(不封装，纯编码任务)</remarks>
        private string containerFormat;

        public string ContainerFormat
        {
            get { return containerFormat; }
            set {
                containerFormat = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ContainerFormat"));
            }
        }

        /// <summary>
        /// 帧率
        /// </summary>
        private double fps;

        public double Fps
        {
            get { return fps; }
            set {
                fps = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Fps"));
            }
        }

        public uint FpsNum { get; set; }
        public uint FpsDen { get; set; }

        /// <summary>
        /// 视频编码格式
        /// </summary>
        private string videoFormat;

        public string VideoFormat
        {
            get { return videoFormat; }
            set {
                videoFormat = value;
                OnPropertyChanged(new PropertyChangedEventArgs("VideoFormat"));
            }
        }

        /// <summary>
        /// 音频编码格式
        /// </summary>
        /// <remarks>e.g. FLAC, AAC</remarks>
        private string audioFormat;

        // 只显示第一条音轨
        public string AudioFormat
        {
            get {
                // 无损不显示码率
                if (audioFormat.ToLower() == "flac" || audioFormat.ToLower() == "alac")
                {
                    return audioFormat;
                }

                if (AudioTracks.Count == 0)
                {
                    return audioFormat;
                }
                return audioFormat + " " + AudioTracks[0].Bitrate.ToString();
            }
            set {
                audioFormat = value;
                OnPropertyChanged(new PropertyChangedEventArgs("AudioFormat"));
            }
        }

        /// <summary>
        /// 音频轨道
        /// </summary>
        private ObservableCollection<AudioInfo> audioTracks;

        public ObservableCollection<AudioInfo> AudioTracks
        {
            get {
                if (audioTracks == null)
                {
                    audioTracks = new ObservableCollection<AudioInfo>();
                }

                return audioTracks;
            }
        }

        /// <summary>
        /// 输入脚本
        /// </summary>
        private string inputScript;

        public string InputScript
        {
            get { return inputScript; }
            set {
                inputScript = value;
                OnPropertyChanged(new PropertyChangedEventArgs("InputScript"));
            }
        }

        /// <summary>
        /// 编码器路径
        /// </summary>
        private string encoderPath;

        public string EncoderPath
        {
            get { return encoderPath; }
            set {
                encoderPath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EncoderPath"));
            }
        }

        /// <summary>
        /// 编码参数
        /// </summary>
        private string encoderParam;

        public string EncoderParam
        {
            get { return encoderParam; }
            set {
                encoderParam = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EncoderParam"));
            }
        }

        /// <summary>
        /// 是否只抽取(并转码)音轨
        /// </summary>
        private bool isExtAudioOnly;

        public bool IsExtAudioOnly
        {
            get { return isExtAudioOnly; }
            set {
                isExtAudioOnly = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsExtAudioOnly"));
            }
        }

        /// <summary>
        /// 是否包含字幕
        /// </summary>
        private bool includeSub;

        public bool IncludeSub
        {
            get { return includeSub; }
            set {
                includeSub = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IncludeSub"));
            }
        }

        public string SubtitleLanguage;

        #endregion JobConfig

        public TaskDetail()
        {
        }

        public TaskDetail(bool isEnabled, string tid, string taskName, string inputFile, string outputFile,
            string status, double progress, string speed, TimeSpan timeRemain)
        {
            IsEnabled = isEnabled;
            Tid = tid;
            TaskName = taskName;
            InputFile = inputFile;
            OutputFile = outputFile;
            CurrentStatus = status;
            ProgressValue = progress;
            ProgressStr = progress.ToString() + "%";
            Speed = speed;
            TimeRemain = timeRemain;
        }

        // 自动生成输出文件名
        public bool UpdateOutputFileName()
        {
            if (this.VideoFormat == "" || this.InputFile == "")
            {
                return false;
            }

            var finfo = new System.IO.FileInfo(this.InputFile);
            this.OutputFile = finfo.Name + "." + this.VideoFormat.ToLower();
            if (this.ContainerFormat != "")
            {
                this.OutputFile = finfo.Name + "." + this.ContainerFormat.ToLower();
            }

            return true;
        }
    }
}
