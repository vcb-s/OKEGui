using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using OKEGui.Utils;
using OKEGui.Model;
using OKEGui.JobProcessor;

namespace OKEGui.Worker
{
    // TODO: 目前只考虑压制全部任务；以后可能会各步骤分开进行，或者进行其他任务
    // TODO: TaskManger 做成接口。各种不同类型任务分开管理。
    public enum WorkerType
    {
        Normal,
        Temporary,
    }

    internal struct WorkerArgs
    {
        public string Name;
        public WorkerType RunningType;
        public TaskManager taskManager;
        public BackgroundWorker bgWorker;
        public int numaNode;
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
            lock (o)
            {
                if (workerList.Count == 0)
                {
                    return false;
                }

                isRunning = true;

                foreach (string worker in workerList)
                {
                    if (bgworkerlist.ContainsKey(worker))
                    {
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

            if (isRunning)
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

            if (isRunning)
            {
                CreateWorker(name);
                StartWorker(name);
            }

            return true;
        }

        public bool DeleteWorker(string name)
        {
            if (isRunning)
            {
                return false;
            }

            lock (o)
            {
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

            if (bgworkerlist.ContainsKey(name))
            {
                if (bgworkerlist[name].IsBusy)
                {
                    bgworkerlist[name].CancelAsync();
                }
            }
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (WorkerArgs)e.Argument;

            while (isRunning)
            {
                TaskDetail task = args.taskManager.GetNextTask();

                // 检查是否已经完成全部任务
                if (task == null)
                {
                    // 全部工作完成
                    lock (o)
                    {
                        BackgroundWorker v;
                        bgworkerlist.TryRemove(args.Name, out v);

                        if (bgworkerlist.Count == 0 && workerType.Count == 0)
                        {
                            if (AfterFinish != null)
                            {
                                AfterFinish();
                            }
                        }
                    }
                    return;
                }

                JobProfile profile = task.Profile;
                try
                {
                    task.WorkerName = args.Name;
                    task.IsEnabled = false;
                    task.IsRunning = true;

                    // 新建工作
                    // 抽取音轨
                    FileInfo eacInfo = new FileInfo(".\\tools\\eac3to\\eac3to.exe");
                    if (!eacInfo.Exists)
                    {
                        OKETaskException ex = new OKETaskException(Constants.eac3toMissingSmr);
                        ex.progress = 0.0;
                        throw ex;
                    }
                    MediaFile srcTracks = new EACDemuxer(eacInfo.FullName, task.InputFile, profile).Extract(
                        (double progress, EACProgressType type) =>
                        {
                            switch (type)
                            {
                                case EACProgressType.Analyze:
                                    task.CurrentStatus = "轨道分析中";
                                    task.ProgressValue = progress;
                                    break;

                                case EACProgressType.Process:
                                    task.CurrentStatus = "抽取音轨中";
                                    task.ProgressValue = progress;
                                    break;

                                case EACProgressType.Completed:
                                    task.CurrentStatus = "音轨抽取完毕";
                                    task.ProgressValue = progress;
                                    break;

                                default:
                                    return;
                            }
                        });

                    // 新建音频处理工作
                    for (int id = 0; id < srcTracks.AudioTracks.Count; id++)
                    {
                        AudioTrack track = srcTracks.AudioTracks[id];
                        OKEFile file = track.File;
                        AudioInfo info = track.Info as AudioInfo;
                        MuxOption option = info.MuxOption;
                        switch (option)
                        {
                            case MuxOption.Default:
                            case MuxOption.Mka:
                            case MuxOption.External:
                                AudioJob audioJob = new AudioJob(info);
                                audioJob.SetUpdate(task);
                                audioJob.Input = file.GetFullPath();
                                audioJob.Output = Path.ChangeExtension(audioJob.Input, "." + audioJob.CodecString.ToLower());

                                task.JobQueue.Enqueue(audioJob);
                                break;
                            default:
                                break;
                        }
                    }

                    // 新建视频处理工作
                    VideoJob videoJob = new VideoJob(profile.VideoFormat);
                    videoJob.SetUpdate(task);

                    videoJob.Input = profile.InputScript;
                    videoJob.EncoderPath = profile.Encoder;
                    videoJob.EncodeParam = profile.EncoderParam;
                    videoJob.Fps = profile.Fps;
                    videoJob.FpsNum = profile.FpsNum;
                    videoJob.FpsDen = profile.FpsDen;
                    videoJob.NumaNode = args.numaNode;

                    if (profile.VideoFormat == "HEVC")
                    {
                        videoJob.Output = new FileInfo(task.InputFile).FullName + ".hevc";
                        if (!profile.EncoderParam.ToLower().Contains("--pools"))
                        {
                            videoJob.EncodeParam += " --pools " + NumaNode.X265PoolsParam(videoJob.NumaNode);
                        }
                    }
                    else
                    {
                        videoJob.Output = new FileInfo(task.InputFile).FullName;
                        videoJob.Output += profile.ContainerFormat == "MKV" ? "_.mkv" : ".h264";
                        if (!profile.EncoderParam.ToLower().Contains("--threads") && NumaNode.UsableCoreCount > 10)
                        {
                            videoJob.EncodeParam += " --threads 16";
                        }
                    }

                    task.JobQueue.Enqueue(videoJob);

                    // 添加字幕文件
                    foreach (SubtitleTrack track in srcTracks.SubtitleTracks)
                    {
                        OKEFile outputFile = track.File;
                        Info info = track.Info;
                        switch (info.MuxOption)
                        {
                            case MuxOption.Default:
                                task.MediaOutFile.AddTrack(track);
                                break;
                            case MuxOption.Mka:
                                task.MkaOutFile.AddTrack(track);
                                break;
                            case MuxOption.External:
                                outputFile.AddCRC32();
                                break;
                            default:
                                break;
                        }
                    }

                    while (task.JobQueue.Count != 0)
                    {
                        Job job = task.JobQueue.Dequeue();

                        if (job is AudioJob)
                        {
                            AudioJob audioJob = job as AudioJob;
                            AudioInfo info = audioJob.Info as AudioInfo;
                            string srcFmt = Path.GetExtension(audioJob.Input).ToUpper().Remove(0, 1);
                            if (srcFmt == "FLAC" && audioJob.CodecString == "AAC")
                            {
                                task.CurrentStatus = "音频转码中";
                                task.IsUnKnowProgress = true;

                                QAACEncoder qaac = new QAACEncoder(audioJob, info.Bitrate);

                                qaac.start();
                                qaac.waitForFinish();
                            }
                            else if (srcFmt != audioJob.CodecString)
                            {
                                OKETaskException ex = new OKETaskException(Constants.audioFormatMistachSmr);
                                ex.Data["SRC_FMT"] = srcFmt;
                                ex.Data["DST_FMT"] = audioJob.CodecString;
                                throw ex;
                            }

                            OKEFile outputFile = new OKEFile(job.Output);
                            switch (info.MuxOption)
                            {
                                case MuxOption.Default:
                                    task.MediaOutFile.AddTrack(new AudioTrack(outputFile, info));
                                    break;
                                case MuxOption.Mka:
                                    task.MkaOutFile.AddTrack(new AudioTrack(outputFile, info));
                                    break;
                                case MuxOption.External:
                                    outputFile.AddCRC32();
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (job is VideoJob)
                        {
                            CommandlineVideoEncoder processor;
                            task.CurrentStatus = "获取信息中";
                            task.IsUnKnowProgress = true;
                            if (job.CodecString == "HEVC")
                            {
                                processor = new X265Encoder(job);
                            }
                            else
                            {
                                processor = new X264Encoder(job);
                            }
                            task.CurrentStatus = "压制中";
                            task.ProgressValue = 0.0;
                            processor.start();
                            processor.waitForFinish();

                            videoJob = job as VideoJob;
                            VideoInfo info = new VideoInfo(videoJob.FpsNum, videoJob.FpsDen);

                            task.MediaOutFile.AddTrack(new VideoTrack(new OKEFile(job.Output), info));
                        }
                        else
                        {
                            // 不支持的工作
                        }
                    }

                    // 添加章节文件
                    FileInfo txtChapter = new FileInfo(Path.ChangeExtension(task.InputFile, ".txt"));
                    if (txtChapter.Exists)
                    {
                        task.MediaOutFile.AddTrack(new ChapterTrack(new OKEFile(txtChapter)));
                    }


                    // 封装
                    if (profile.ContainerFormat != "")
                    {
                        task.CurrentStatus = "封装中";
                        FileInfo mkvInfo = new FileInfo(".\\tools\\mkvtoolnix\\mkvmerge.exe");
                        if (!mkvInfo.Exists)
                        {
                            throw new Exception("mkvmerge不存在");
                        }

                        FileInfo lsmash = new FileInfo(".\\tools\\l-smash\\muxer.exe");
                        if (!lsmash.Exists)
                        {
                            throw new Exception("l-smash 封装工具不存在");
                        }

                        AutoMuxer muxer = new AutoMuxer(mkvInfo.FullName, lsmash.FullName);
                        muxer.ProgressChanged += progress => task.ProgressValue = progress;

                        muxer.StartMuxing(Path.GetDirectoryName(task.InputFile) + "\\" + task.OutputFile, task.MediaOutFile);
                    }
                    if (task.MkaOutFile.Tracks.Count > 0)
                    {
                        task.CurrentStatus = "封装MKA中";
                        FileInfo mkvInfo = new FileInfo(".\\tools\\mkvtoolnix\\mkvmerge.exe");
                        FileInfo lsmash = new FileInfo(".\\tools\\l-smash\\muxer.exe");
                        AutoMuxer muxer = new AutoMuxer(mkvInfo.FullName, lsmash.FullName);
                        muxer.ProgressChanged += progress => task.ProgressValue = progress;
                        string mkaOutputFile = task.InputFile + ".mka";

                        muxer.StartMuxing(mkaOutputFile, task.MkaOutFile);
                    }

                    task.CurrentStatus = "完成";
                    task.ProgressValue = 100;
                }
                catch (OKETaskException ex)
                {
                    ExceptionMsg msg = ExceptionParser.Parse(ex, task);
                    new System.Threading.Tasks.Task(() =>
                    System.Windows.MessageBox.Show(msg.errorMsg, msg.fileName)).Start();
                    task.IsRunning = false;
                    task.CurrentStatus = ex.summary;
                    task.ProgressValue = ex.progress.GetValueOrDefault(task.ProgressValue);
                    continue;
                }
                catch (Exception ex)
                {
                    FileInfo fileinfo = new FileInfo(task.InputFile);
                    new System.Threading.Tasks.Task(() =>
                            System.Windows.MessageBox.Show(ex.Message, fileinfo.Name)).Start();
                    task.IsRunning = false;
                    task.CurrentStatus = "未知错误";
                    continue;
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
