using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using OKEGui.Utils;
using OKEGui.Model;
using OKEGui.JobProcessor;
using static OKEGui.RpChecker;
using TChapter.Chapters;

namespace OKEGui.Worker
{
    // 单独将worker执行task的函数分离出来。其余关于worker的其他定义，见WorkerManager
    // TODO: 改写为更模块化的函数。
    public partial class WorkerManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("WorkerManager");

        private void DisableButtonsAfterFinish(MainWindow window)
        {
            window.BtnPause.IsEnabled = false;
            window.BtnResume.IsEnabled = false;
            window.BtnEmpty.IsEnabled = true;
            window.BtnMoveDown.IsEnabled = false;
            window.BtnMoveUp.IsEnabled = false;
            window.BtnMoveTop.IsEnabled = false;
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

                    Logger.Info("-------------------------------------------------------------------");
                    Logger.Info($"开始处理任务：{task.InputFile}");

                    // 获取视频信息、检查脚本
                    VSPipeInfo vsInfo = GetVSPipeInfo(task, profile);

                    // 准备时间码/章节文件/qpfile
                    VideoInfo finalVideoInfo = DoPreparation(task, profile, vsInfo);

                    // 处理ReEncode任务
                    if (profile.isReEncode)
                    {
                        CheckReEncodeSlice(task, profile);
                    }
                    // 处理常规压制任务
                    else
                    {
                        // 抽取音轨和字幕轨
                        MediaFile srcTracks = ExtractSource(task, profile);

                        // 新建音频处理工作
                        GenerateAudioJob(task, srcTracks);

                        // 新建视频处理工作
                        GenerateVideoJob(task, profile, args.numaNode, finalVideoInfo);

                        // 添加字幕文件
                        AddSubtitle(task, srcTracks);

                        // 执行音频和视频处理工作
                        DoAllJobs(task, profile);

                        // 最终封装
                        DoFinalMux(task, profile);

                        // RPC检查
                        DoRPCheck(task, profile);
                    }

                    Logger.Info("任务完成");
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


        // 获取视频信息、检查脚本
        private VSPipeInfo GetVSPipeInfo(TaskDetail task, TaskProfile profile)
        {
            task.CurrentStatus = "获取信息中";
            task.IsUnKnowProgress = true;

            VideoInfoJob viJob = new VideoInfoJob();
            viJob.SetUpdate(task);
            viJob.Input = profile.InputScript;
            viJob.IsReEncode = profile.isReEncode;
            viJob.Vfr = profile.TimeCode;
            viJob.FpsNum = profile.FpsNum;
            viJob.FpsDen = profile.FpsDen;
            if (profile.Config != null)
            {
                viJob.VspipeArgs.AddRange(profile.Config.VspipeArgs);
            }

            VSPipeInfo vsInfo = new VSPipeInfo(viJob);
            task.NumberOfFrames = vsInfo.videoInfo.numFrames;

            Logger.Info($"获取信息完成：VFR: {vsInfo.videoInfo.vfr}, FPS: {vsInfo.videoInfo.fps}, " +
                        $"FpsNum: {vsInfo.videoInfo.fpsNum}, FpsDen: {vsInfo.videoInfo.fpsDen}, NumFrames: {task.NumberOfFrames}");

            return vsInfo;
        }

        // 准备时间码/章节文件/qpfile
        private VideoInfo DoPreparation(TaskDetail task, TaskProfile profile, VSPipeInfo vsInfo)
        {
            // 时间码文件
            Timecode timecode = null;
            string timeCodeFile = null;
            if (vsInfo.videoInfo.vfr)
            {
                timecode = new Timecode(Path.ChangeExtension(task.InputFile, ".tcfile"),
                                        (int) task.NumberOfFrames);
                timeCodeFile = Path.ChangeExtension(task.InputFile, ".v2.tcfile");
                File.Delete(timeCodeFile);
                try
                {
                    timecode.SaveTimecode(timeCodeFile);
                }
                catch (IOException ex)
                {
                    Logger.Info($"无法写入修正后timecode，将使用原文件\n{ex.Message}");
                    timeCodeFile = Path.ChangeExtension(task.InputFile, ".tcfile");
                }

                task.LengthInMiliSec = (long) (timecode.TotalLength.Ticks / 1e4 + 0.5);
            }
            else
            {
                task.LengthInMiliSec = (long) ((task.NumberOfFrames - 1) / vsInfo.videoInfo.fps * 1000 + 0.5);
            }

            // 添加章节文件
            string qpFileName = null;
            string qpFile = null;
            if (!profile.isReEncode)
            {
                ChapterInfo chapterInfo = ChapterService.LoadChapter(task);
                if (chapterInfo != null)
                {
                    if (task.ChapterStatus == ChapterStatus.Maybe || task.ChapterStatus == ChapterStatus.MKV)
                    {
                        task.ChapterStatus = ChapterStatus.Added;
                    }

                    FileInfo outputChapterFile = new FileInfo(Path.ChangeExtension(task.Taskfile.WorkingPathPrefix, ".txt"));
                    chapterInfo.Save(ChapterTypeEnum.OGM, outputChapterFile.FullName);
                    outputChapterFile.Refresh();
                    OKEFile chapterFile = new OKEFile(outputChapterFile);
                    task.MediaOutFile.AddTrack(new ChapterTrack(chapterFile));
                    task.MediaOutFile.ChapterLanguage = task.ChapterLanguage;

                    // 用章节文件生成qpfile
                    qpFileName = Path.ChangeExtension(task.Taskfile.WorkingPathPrefix, ".qpf");
                    qpFile = vsInfo.videoInfo.vfr
                        ? ChapterService.GenerateQpFile(chapterInfo, timecode)
                        : ChapterService.GenerateQpFile(chapterInfo, vsInfo.videoInfo.fps);
                    if (profile.VideoFormat == "AV1")
                    {
                        qpFile = String.Join(",", qpFile.Replace(" I", "f").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    else
                    {
                        File.WriteAllText(qpFileName, qpFile);
                        qpFile = qpFileName;
                    }
                }
                else
                    task.ChapterStatus = ChapterStatus.No;
            }

            VideoInfo info = new VideoInfo(vsInfo.videoInfo.fpsNum, vsInfo.videoInfo.fpsDen, timeCodeFile, qpFile);
            Logger.Info($"准备时间码和章节文件完成：timeCodeFile: {timeCodeFile}, qpFile: {qpFile}");

            return info;
        }

        // 抽取音轨
        private MediaFile ExtractSource(TaskDetail task, TaskProfile profile)
        {
            FileInfo eacInfo = new FileInfo(Constants.eac3toWrapperPath);
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
                            task.CurrentStatus = "音轨已抽取";
                            task.ProgressValue = progress;
                            break;

                        default:
                            return;
                    }
                });
            return srcTracks;
        }

        // 新建音频处理工作
        private void GenerateAudioJob(TaskDetail task, MediaFile srcTracks)
        {
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
        }

        // 新建视频处理工作
        private void GenerateVideoJob(TaskDetail task, TaskProfile profile, int numaNode, VideoInfo info, long begin = -1, long end = -1, int partId = -1)
        {
            VideoJob vJob = new VideoJob(info, profile.VideoFormat);
            vJob.SetUpdate(task);

            vJob.Input = profile.InputScript;
            vJob.EncoderPath = profile.Encoder;
            vJob.EncodeParam = profile.EncoderParam;
            vJob.NumaNode = numaNode;
            vJob.IsPartialEncode = (end != -1);
            vJob.FrameBegin = begin;
            vJob.FrameEnd = end;
            if (vJob.IsPartialEncode)
                vJob.NumberOfFrames = end - begin;
            else
                vJob.NumberOfFrames = task.NumberOfFrames;

            if (profile.Config != null)
                vJob.VspipeArgs.AddRange(profile.Config.VspipeArgs);

            vJob.Output = profile.WorkingPathPrefix + (vJob.IsPartialEncode ? $"_part{partId}" : "");
            if (profile.VideoFormat == "HEVC")
            {
                vJob.Output += ".hevc";
                if (!profile.EncoderParam.ToLower().Contains("--pools"))
                {
                    vJob.EncodeParam += " --pools " + NumaNode.X265PoolsParam(vJob.NumaNode);
                }
            }
            else if (profile.VideoFormat == "AVC")
            {
                vJob.Output += profile.ContainerFormat == "MKV" ? "_.mkv" : ".h264";
                if (!profile.EncoderParam.ToLower().Contains("--threads") && NumaNode.UsableCoreCount > 10)
                {
                    vJob.EncodeParam += " --threads 16";
                }
            }
            else if (profile.VideoFormat == "AV1")
            {
                vJob.Output += ".ivf";
                if (!profile.EncoderParam.ToLower().Contains("--lp") && NumaNode.UsableCoreCount > 8)
                {
                    vJob.EncodeParam += " --lp 8";
                }
            }
            else
            {
                throw new Exception("unknown video codec: " + profile.VideoFormat);
            }

            // 添加qpfile参数
            if (!vJob.IsPartialEncode && vJob.Info.QpFile != null)
            {
                if (vJob.CodecString == "AV1")
                    vJob.EncodeParam += $" --force-key-frames \"{vJob.Info.QpFile}\"";
                else
                    vJob.EncodeParam += $" --qpfile \"{vJob.Info.QpFile}\"";
            }

            task.JobQueue.Enqueue(vJob);
            Logger.Info($"添加压制任务：numFrame: {vJob.NumberOfFrames}, begin: {vJob.FrameBegin}, end: {vJob.FrameEnd}, qpfile: {vJob.Info.QpFile}, output: {vJob.Output}");
        }

        // 添加字幕文件
        private void AddSubtitle(TaskDetail task, MediaFile srcTracks)
        {
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
        }

        // 执行音频和视频处理工作
        private void DoAllJobs(TaskDetail task, TaskProfile profile)
        {
            while (task.JobQueue.Count != 0)
            {
                Job job = task.JobQueue.Dequeue();

                switch (job)
                {
                    case AudioJob aJob:
                    {
                        AudioInfo info = aJob.Info;
                        string srcFmt = Path.GetExtension(aJob.Input).ToUpper().Remove(0, 1);
                        if (srcFmt == "FLAC" && aJob.CodecString == "AAC")
                        {
                            task.CurrentStatus = "音频转码中";

                            QAACEncoder qaac = new QAACEncoder(aJob, (double progress) =>
                            {
                                task.ProgressValue = progress;
                            }, info.Bitrate);

                            qaac.start();
                            qaac.waitForFinish();
                        }
                        else if (srcFmt != "FLAC" && aJob.CodecString == "AAC" && info.Lossy)
                        {
                            task.CurrentStatus = "音频转码中";

                            FFmpegPipeQAACEncoder qaac = new FFmpegPipeQAACEncoder(aJob, (double progress) =>
                            {
                                task.ProgressValue = progress;
                            }, info.Bitrate);

                            qaac.start();
                            qaac.waitForFinish();
                        }
                        else if (srcFmt != aJob.CodecString)
                        {
                            OKETaskException ex = new OKETaskException(Constants.audioFormatMistachSmr);
                            ex.Data["SRC_FMT"] = srcFmt;
                            ex.Data["DST_FMT"] = aJob.CodecString;
                            throw ex;
                        }

                        OKEFile outputFile = new OKEFile(aJob.Output);
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

                        break;
                    }
                    case VideoJob vJob:
                    {
                        CommandlineVideoEncoder processor;
                        if (vJob.CodecString == "HEVC")
                        {
                            processor = new X265Encoder(vJob);
                        }
                        else if (vJob.CodecString == "AVC")
                        {
                            processor = new X264Encoder(vJob);
                        }
                        else if (vJob.CodecString == "AV1")
                        {
                            processor = new SVTAV1Encoder(vJob);
                        }
                        else
                        {
                            throw new Exception("unknown video codec: " + vJob.CodecString);
                        }

                        // 开始压制
                        task.CurrentStatus = "压制中";
                        task.ProgressValue = 0.0;
                        processor.start();
                        processor.waitForFinish();

                        if (!vJob.IsPartialEncode)
                        {
                            task.MediaOutFile.AddTrack(new VideoTrack(new OKEFile(vJob.Output), vJob.Info));
                        }

                        break;
                    }
                    default:
                        // 不支持的工作
                        break;
                }
            }
        }

        // 最终封装
        private void DoFinalMux(TaskDetail task, TaskProfile profile)
        {
            FileInfo mkvInfo = new FileInfo(Constants.mkvmergePath);
            FileInfo lsmash = new FileInfo(Constants.lsmashPath);
            if (!mkvInfo.Exists)
                throw new Exception("mkvmerge不存在");
            if (!lsmash.Exists)
                throw new Exception("l-smash 封装工具不存在");

            if (profile.ContainerFormat != "")
            {
                task.CurrentStatus = "封装中";
                AutoMuxer muxer = new AutoMuxer(mkvInfo.FullName, lsmash.FullName);
                muxer.ProgressChanged += (progress => task.ProgressValue = progress);

                OKEFile outFile = muxer.StartMuxing(
                    Path.Combine(Path.GetDirectoryName(profile.OutputPathPrefix), task.OutputFile),
                    task.MediaOutFile
                );
                task.OutputFile = outFile.GetFileName();
                task.BitRate = CommandlineVideoEncoder.HumanReadableFilesize(outFile.GetFileSize(), 2);
            }
            if (task.MkaOutFile.Tracks.Count > 0)
            {
                task.CurrentStatus = "封装MKA中";
                AutoMuxer muxer = new AutoMuxer(mkvInfo.FullName, lsmash.FullName);
                muxer.ProgressChanged += (progress => task.ProgressValue = progress);

                string mkaOutputFile = profile.OutputPathPrefix + ".mka";
                muxer.StartMuxing(mkaOutputFile, task.MkaOutFile);
            }
        }

        // RPC检查
        private void DoRPCheck(TaskDetail task, TaskProfile profile)
        {
            if (profile.Rpc)
            {
                List<string> vspipeArgs = new List<string>();
                if (profile.Config != null)
                    vspipeArgs.AddRange(profile.Config.VspipeArgs);

                task.CurrentStatus = "RPC中";
                RpcJob rpcJob = new RpcJob(
                    profile.InputScript,
                    profile.OutputPathPrefix,
                    task.MediaOutFile.VideoTrack.File.GetFullPath(),
                    task.NumberOfFrames,
                    vspipeArgs
                );
                rpcJob.SetUpdate(task);

                RpChecker checker = new RpChecker(rpcJob);
                checker.start();
                checker.waitForFinish();
                task.RpcOutput = rpcJob.Output;
            }
            else
            {
                task.RpcStatus = RpcStatus.跳过.ToString();
            }
        }


        // 获取ReEncode旧成品的I帧序列，生成最终的切片序列
        private void CheckReEncodeSlice(TaskDetail task, TaskProfile profile)
        {
            Logger.Debug("DoCheckReEncodeSlice");
            Logger.Debug("epConfig: " + profile.Config.ToString());
        }
    }
}
