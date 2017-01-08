using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;

namespace OKEGui
{
    //继承自INotifyPropertyChanged接口来实现数据的实时显示
    public class JobDetails : INotifyPropertyChanged
    {
        // 任务设置

        #region JobConfig

        private bool isEnabled;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set {
                isEnabled = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsEnabled"));
            }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set {
                isRunning = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsRunning"));
            }
        }

        private string tid;

        public string Tid
        {
            get { return tid; }
            set {
                tid = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Tid"));
            }
        }

        private string taskName;

        public string TaskName
        {
            get { return taskName; }
            set {
                taskName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TaskName"));
            }
        }

        private string workerName;

        public string WorkerName
        {
            get { return workerName; }
            set {
                workerName = value;
                OnPropertyChanged(new PropertyChangedEventArgs("WorkerName"));
            }
        }

        private string inputFile;

        public string InputFile
        {
            get { return inputFile; }
            set {
                inputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("InputFile"));
            }
        }

        // 最终成品 c:\xxx\123.mkv
        private string outputFile;

        public string OutputFile
        {
            get { return outputFile; }
            set {
                outputFile = value;
                OnPropertyChanged(new PropertyChangedEventArgs("OutputFile"));
            }
        }

        // mp4, mkv, null
        private string containerFormat;

        public string ContainerFormat
        {
            get { return containerFormat; }
            set {
                containerFormat = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ContainerFormat"));
            }
        }

        // HEVC, AVC
        private string videoFormat;

        public string VideoFormat
        {
            get { return videoFormat; }
            set {
                videoFormat = value;
                OnPropertyChanged(new PropertyChangedEventArgs("VideoFormat"));
            }
        }

        // FLAC, AAC(m4a)
        private string audioFormat;

        // 只显示第一条音轨
        public string AudioFormat
        {
            get {
                // 无损不显示码率
                if (audioFormat.ToLower() == "flac" || audioFormat.ToLower() == "alac") {
                    return audioFormat;
                }

                if (AudioTracks.Count == 0) {
                    return audioFormat;
                }
                return audioFormat + " " + AudioTracks[0].Bitrate.ToString();
            }
            set {
                audioFormat = value;
                OnPropertyChanged(new PropertyChangedEventArgs("AudioFormat"));
            }
        }

        // 音轨信息
        public class AudioInfo
        {
            public int TrackId { get; set; }
            public string Format { get; set; }
            public int Bitrate { get; set; }
            public string ExtraArg { get; set; }
            public bool IsMux { get; set; }
        }

        private ObservableCollection<AudioInfo> audioTracks;

        public ObservableCollection<AudioInfo> AudioTracks
        {
            get {
                if (audioTracks == null) {
                    audioTracks = new ObservableCollection<AudioInfo>();
                }

                return audioTracks;
            }
        }

        private string status;

        public string Status
        {
            get { return status; }
            set {
                status = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Status"));
            }
        }

        private double progress;

        public double ProgressValue
        {
            get { return progress; }
            set {
                progress = value;
                // Indetermate
                if (progress < 0) {
                    ProgressStr = "";
                    IsUnKnowProgress = true;
                } else {
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

        private bool isUnKnowProgress;

        public bool IsUnKnowProgress
        {
            get { return isUnKnowProgress; }
            set {
                isUnKnowProgress = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsUnKnowProgress"));
            }
        }

        private string speed;

        public string Speed
        {
            get { return speed; }
            set {
                speed = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Speed"));
            }
        }

        private string bitrate;

        public string BitRate
        {
            get { return bitrate; }
            set {
                bitrate = value;
                OnPropertyChanged(new PropertyChangedEventArgs("BitRate"));
            }
        }

        private TimeSpan timeRemain;

        public TimeSpan TimeRemain
        {
            get { return timeRemain; }
            set {
                timeRemain = value;
                TimeRemainStr = value.ToString(@"hh\:mm\:ss");
                if (value.TotalHours > 24.0) {
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

        // Task 详细设置
        private string inputScript;

        public string InputScript
        {
            get { return inputScript; }
            set {
                inputScript = value;
                OnPropertyChanged(new PropertyChangedEventArgs("InputScript"));
            }
        }

        private string encoderPath;

        public string EncoderPath
        {
            get { return encoderPath; }
            set {
                encoderPath = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EncoderPath"));
            }
        }

        private string encoderParam;

        public string EncoderParam
        {
            get { return encoderParam; }
            set {
                encoderParam = value;
                OnPropertyChanged(new PropertyChangedEventArgs("EncoderParam"));
            }
        }

        private bool isExtAudioOnly;

        public bool IsExtAudioOnly
        {
            get { return isExtAudioOnly; }
            set {
                isExtAudioOnly = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsExtAudioOnly"));
            }
        }

        #endregion JobConfig

        public JobDetails()
        {
        }

        public JobDetails(bool isEnabled, string tid, string taskName, string inputFile, string outputFile,
            string status, double progress, string speed, TimeSpan timeRemain)
        {
            IsEnabled = isEnabled;
            Tid = tid;
            TaskName = taskName;
            InputFile = inputFile;
            OutputFile = outputFile;
            Status = status;
            ProgressValue = progress;
            ProgressStr = progress.ToString() + "%";
            Speed = speed;
            TimeRemain = timeRemain;
        }

        // 自动生成输出文件名
        public bool UpdateOutputFileName()
        {
            if (this.VideoFormat == "" || this.InputFile == "") {
                return false;
            }

            var finfo = new System.IO.FileInfo(this.InputFile);
            this.OutputFile = finfo.Name + "." + this.VideoFormat.ToLower();
            if (this.ContainerFormat != "") {
                this.OutputFile = finfo.Name + "." + this.ContainerFormat.ToLower();
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
    }

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
        public MTObservableCollection<JobDetails> taskStatus = new MTObservableCollection<JobDetails>();

        private int newTaskCount = 1;
        private int tidCount = 0;

        public bool isCanStart = false;
        private object o = new object();

        public bool CheckTask(JobDetails td)
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
            taskStatus.Add(new JobDetails(false, (taskStatus.Count + 1).ToString(), "新建任务 - " + newTaskCount.ToString(),
                "", "", "需要修改", 0.0, "0.0 fps", TimeSpan.FromDays(30)));

            newTaskCount = newTaskCount + 1;

            return taskStatus.Count;
        }

        public int AddTask(JobDetails detail)
        {
            JobDetails td = detail;

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
            td.Status = "等待中";
            td.ProgressValue = 0.0;
            td.Speed = "0.0 fps";
            td.TimeRemain = TimeSpan.FromDays(30);
            td.WorkerName = "";

            taskStatus.Add(td);

            return taskStatus.Count;
        }

        public bool DeleteTask(JobDetails detail)
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

        public bool UpdateTask(JobDetails detail)
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

        public VideoJob GetNextJob()
        {
            if (!isCanStart) {
                return null;
            }

            lock (o) {
                VideoJob vj = new VideoJob("HEVC");

                // 找出下一个可用任务
                foreach (var job in taskStatus) {
                    if (job.IsEnabled) {
                        job.IsEnabled = false;
                        job.IsRunning = true;
                        vj.config = job;
                        vj.Input = job.InputScript;
                        vj.Output = Path.GetDirectoryName(job.InputFile) + "\\" + job.OutputFile;
                        return vj;
                    }
                }
            }

            return null;
        }

        public bool UpdateTaskName(int tid, string taskName)
        {
            if (tid < 1) {
                return false;
            }

            if (taskName == "") {
                return false;
            }

            taskStatus[tid].TaskName = taskName;
            return true;
        }

        public bool UpdateTaskOutputFile(int tid, string outputFile)
        {
            if (tid < 1) {
                return false;
            }

            if (outputFile == "") {
                return false;
            }

            taskStatus[tid].OutputFile = outputFile;
            return true;
        }

        public bool EnableTask(int tid, bool isEnable = true)
        {
            if (tid < 1) {
                return false;
            }

            taskStatus[tid].IsEnabled = isEnable;

            return true;
        }

        public bool UpdateTaskStatus(int tid, string status)
        {
            if (tid < 1) {
                return false;
            }

            if (status == "") {
                return false;
            }

            taskStatus[tid].Status = status;
            return true;
        }

        public bool UpdateTaskProgress(int tid, double progress)
        {
            if (tid < 1) {
                return false;
            }

            if (progress < 0) {
                return false;
            }

            taskStatus[tid].ProgressValue = progress;
            return true;
        }

        public bool UpdateTaskSpeed(int tid, string speed)
        {
            if (tid < 1) {
                return false;
            }

            if (speed == "") {
                return false;
            }

            taskStatus[tid].Speed = speed;
            return true;
        }

        public bool UpdateTaskTimeRemain(int tid, TimeSpan time)
        {
            if (tid < 1) {
                return false;
            }

            if (time.Ticks < 0) {
                return false;
            }

            taskStatus[tid].TimeRemain = time;
            return true;
        }

        public bool UpdateTaskEncoderPath(int tid, string path)
        {
            if (tid < 1) {
                return false;
            }

            if (path == "") {
                return false;
            }

            taskStatus[tid].EncoderPath = path;
            return true;
        }

        public bool UpdateTaskEncoderParam(int tid, string param)
        {
            if (tid < 1) {
                return false;
            }

            if (param == "") {
                return false;
            }

            taskStatus[tid].EncoderParam = param;
            return true;
        }

        public bool UpdateTaskIsExtAudioOnly(int tid, bool isExtAudioOnly)
        {
            if (tid < 1) {
                return false;
            }

            taskStatus[tid].IsExtAudioOnly = isExtAudioOnly;
            return true;
        }
    }
}
