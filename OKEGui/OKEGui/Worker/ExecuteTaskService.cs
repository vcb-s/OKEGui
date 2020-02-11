using System;
using System.ComponentModel;
using System.IO;
using OKEGui.Utils;
using OKEGui.Model;
using OKEGui.JobProcessor;
using System.Diagnostics;
using System.Collections.Generic;
using static OKEGui.RpChecker;

namespace OKEGui.Worker
{
    // 单独将worker执行task的函数分离出来。其余关于worker的其他定义，见WorkerManager
    // TODO: 改写为更模块化的函数。
    public partial class WorkerManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private void DisableButtonsAfterFinish(MainWindow window)
        {
            window.BtnPause.IsEnabled = false;
            window.BtnResume.IsEnabled = false;
            window.BtnEmpty.IsEnabled = true;
            window.BtnMoveDown.IsEnabled = false;
            window.BtnMoveUp.IsEnabled = false;
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgs args = (WorkerArgs)e.Argument;

            while (IsRunning)
            {
                TaskDetail task = args.taskManager.GetNextTask();

                // 检查是否已经完成全部任务
                if (task == null)
                {
                    Logger.Debug("所有任务已经完成");
                    Action<MainWindow> disableButtonsAction = new Action<MainWindow>(DisableButtonsAfterFinish);
                    MainWindow.Dispatcher.BeginInvoke(disableButtonsAction, MainWindow);
                    lock (o)
                    {
                        bgworkerlist.TryRemove(args.Name, out BackgroundWorker v);
                        if (bgworkerlist.Count == 0)
                        {
                            IsRunning = false;

                            if (args.taskManager.AllSuccess())
                            {
                                if (AfterFinish != null)
                                {
                                    Logger.Info("全部任务正常结束；准备执行完结命令。");
                                    MainWindow.Dispatcher.BeginInvoke(AfterFinish, MainWindow);
                                }
                                else
                                {
                                    Logger.Info("全部任务正常结束；没有完结命令。");
                                }
                            }
                            else
                            {
                                Logger.Info("有些任务未正常结束，不执行完结命令。");
                            }
                        }
                    }
                    return;
                }

                TaskProfile profile = task.Taskfile;
                try
                {
                    task.WorkerName = args.Name;
                    task.Progress = TaskStatus.TaskProgress.RUNNING;
                    task.MediaOutFile = new MediaFile();
                    task.MkaOutFile = new MediaFile();

                    // 抽取音轨
                    profile.ExtractVideo = VideoService.ForceExtractVideo(task.InputFile);
                    FileInfo eacInfo = new FileInfo(".\\tools\\eac3to\\eac3to.exe");
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

                    // 如果必须抽取视频轨道，这里需要修改输入的vpy文件
                    if (profile.ExtractVideo)
                    {
                        VideoService.ReplaceVpyInputFile(profile.InputScript, task.InputFile, srcTracks.VideoTrack.File.GetFullPath());
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
                    if (profile.Config != null)
                    {
                        videoJob.VspipeArgs.AddRange(profile.Config.VspipeArgs);
                    }

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
                            videoJob = job as VideoJob;
                            CommandlineVideoEncoder processor;
                            task.CurrentStatus = "获取信息中";
                            task.IsUnKnowProgress = true;
                            if (job.CodecString == "HEVC")
                            {
                                processor = new X265Encoder(videoJob);
                            }
                            else
                            {
                                processor = new X264Encoder(videoJob);
                            }

                            task.lengthInMiliSec = (long)((processor.NumberOfFrames - 1) / videoJob.Fps * 1000 + 0.5);

                            // 添加章节文件
                            OKEFile chapterFile = ChapterService.AddChapter(task);
                            if (chapterFile != null)
                            {
                                // 用章节文件生成qpfile
                                string qpFileName = Path.ChangeExtension(task.InputFile, ".qpf");
                                string qpFile = ChapterService.GenerateQpFile(chapterFile, videoJob.Fps);
                                File.WriteAllText(qpFileName, qpFile);
                                processor.AppendParameter($"--qpfile \"{qpFileName}\"");
                            }

                            // 开始压制
                            task.CurrentStatus = "压制中";
                            task.ProgressValue = 0.0;
                            processor.start();
                            processor.waitForFinish();

                            VideoInfo info = new VideoInfo(videoJob.FpsNum, videoJob.FpsDen);

                            task.MediaOutFile.AddTrack(new VideoTrack(new OKEFile(job.Output), info));
                        }
                        else
                        {
                            // 不支持的工作
                        }
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

                    //RP check
                    if (profile.Rpc)
                    {
                        task.CurrentStatus = "RPC中";
                        RpcJob rpcJob = new RpcJob(profile.InputScript, videoJob.Output, videoJob.NumberOfFrames);
                        rpcJob.SetUpdate(task);

                        RpChecker checker = new RpChecker(rpcJob);
                        checker.start();
                        checker.waitForFinish();
                    }
                    else
                    {
                        task.RpcStatus = RpcStatus.跳过.ToString();
                    }

                    task.CurrentStatus = "完成";
                    task.Progress = TaskStatus.TaskProgress.FINISHED;
                    task.ProgressValue = 100;
                }
                catch (OKETaskException ex)
                {
                    ExceptionMsg msg = ExceptionParser.Parse(ex, task);
                    Logger.Error(msg);
                    new System.Threading.Tasks.Task(() =>
                    System.Windows.MessageBox.Show(msg.errorMsg, msg.fileName)).Start();
                    task.Progress = TaskStatus.TaskProgress.ERROR;
                    task.CurrentStatus = ex.summary;
                    task.ProgressValue = ex.progress.GetValueOrDefault(task.ProgressValue);
                    continue;
                }
                catch (Exception ex)
                {
                    FileInfo fileinfo = new FileInfo(task.InputFile);
                    Logger.Error(ex.Message + ex.StackTrace);
                    new System.Threading.Tasks.Task(() =>
                            System.Windows.MessageBox.Show(ex.Message, fileinfo.Name)).Start();
                    task.Progress = TaskStatus.TaskProgress.ERROR;
                    task.CurrentStatus = "未知错误";
                    continue;
                }
            }
        }
    }
}
