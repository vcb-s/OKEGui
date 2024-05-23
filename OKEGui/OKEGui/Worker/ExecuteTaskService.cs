using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using OKEGui.Utils;
using OKEGui.Model;
using OKEGui.JobProcessor;
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
            window.BtnStop.IsEnabled = false;
            window.BtnPause.IsEnabled = false;
            window.BtnResume.IsEnabled = false;
            window.BtnChap.IsEnabled = false;
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
                    Logger.Debug("没有待运行的任务");
                    lock (o)
                    {
                        bgworkerlist.TryRemove(args.Name, out BackgroundWorker v);
                        if (bgworkerlist.Count == 0)
                        {
                            IsRunning = false;
                            Action<MainWindow> disableButtonsAction = new Action<MainWindow>(DisableButtonsAfterFinish);
                            MainWindow.Dispatcher.BeginInvoke(disableButtonsAction, MainWindow);

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
                    if (profile.IsReEncode)
                    {
                        // 根据旧成品的I帧序列，生成最终的切片序列
                        CheckReEncodeSlice(task, profile, vsInfo.iFrameInfo);

                        // 新建各个切片的压制和封装处理工作
                        GenerateReEncodeJob(task, profile, args.numaNode, finalVideoInfo);

                        // 执行音频和视频处理工作
                        DoAllJobs(task, profile);

                        // 最终封装
                        GenerateMuxJob(task, profile, profile.Config.ReEncodeOldFile);

                        // 执行最终封装工作
                        DoAllJobs(task, profile);

                        // RPC检查
                        DoRPCheck(task, profile);
                    }
                    // 处理常规压制任务
                    else
                    {
                        // 抽取音轨和字幕轨
                        MediaFile srcTracks = ExtractSource(task, profile);

                        // 新建音频处理工作
                        GenerateAudioJob(task, srcTracks);

                        // 新建视频处理工作
                        GenerateVideoJob(task, profile, args.numaNode, finalVideoInfo, false);

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
            viJob.IsReEncode = profile.IsReEncode;
            viJob.Vfr = profile.TimeCode;
            viJob.FpsNum = profile.FpsNum;
            viJob.FpsDen = profile.FpsDen;
            if (profile.Config != null)
            {
                viJob.VspipeArgs.AddRange(profile.Config.VspipeArgs);
            }
            if (profile.IsReEncode)
            {
                viJob.WorkingPath = profile.WorkingPathPrefix;
                viJob.ReEncodeOldFile = profile.Config.ReEncodeOldFile;
            }

            VSPipeInfo vsInfo = new VSPipeInfo(viJob);
            task.NumberOfFrames = vsInfo.videoInfo.numFrames;

            Logger.Info($"获取信息完成：VFR: {vsInfo.videoInfo.vfr}, FPS: {vsInfo.videoInfo.fps}, " +
                        $"FpsNum: {vsInfo.videoInfo.fpsNum}, FpsDen: {vsInfo.videoInfo.fpsDen}, NumFrames: {task.NumberOfFrames}");

            if (profile.IsReEncode)
            {
                Logger.Info($"IFrame序列：{vsInfo.iFrameInfo}");
            }

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
                    Logger.Warn($"无法写入修正后timecode，将使用原文件\n{ex.Message}");
                    timeCodeFile = Path.ChangeExtension(task.InputFile, ".tcfile");
                }

                task.LengthInMiliSec = (long) (timecode.TotalLength.Ticks / 1e4 + 0.5);
            }
            else
            {
                task.LengthInMiliSec = (long) ((task.NumberOfFrames - 1) / vsInfo.videoInfo.fps * 1000 + 0.5);
            }

            // 添加章节文件
            IFrameInfo chapterIFrameInfo = null;
            string qpFileName = null;
            string qpFile = null;
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

                chapterIFrameInfo = vsInfo.videoInfo.vfr
                    ? ChapterService.GetChapterIFrameInfo(chapterInfo, timecode)
                    : ChapterService.GetChapterIFrameInfo(chapterInfo, vsInfo.videoInfo.fps);
                qpFile = GenerateQpFile(chapterIFrameInfo, qpFileName, profile.VideoFormat);
            }
            else
                task.ChapterStatus = ChapterStatus.No;

            VideoInfo info = new VideoInfo(vsInfo.videoInfo.fpsNum, vsInfo.videoInfo.fpsDen, timeCodeFile, qpFile, chapterIFrameInfo);
            Logger.Info($"准备时间码和章节文件完成：timeCodeFile: {timeCodeFile}, qpFile: {qpFile}");
            Logger.Info($"chapterIFrameInfo: {info.ChapterIFrameInfo}");

            return info;
        }

        private string GenerateQpFile(IFrameInfo chapterIFrameInfo, string qpFileName, string videoFormat)
        {
            string qpFile = ChapterService.GenerateQpFile(chapterIFrameInfo);

            if (videoFormat == "AV1")
            {
                qpFile = String.Join(",", qpFile.Replace(" I", "f").Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                File.WriteAllText(qpFileName, qpFile);
                qpFile = qpFileName;
            }

            return qpFile;
        }

        // 抽取音轨和字幕轨
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
        private VideoJob GenerateVideoJob(TaskDetail task, TaskProfile profile, int numaNode, VideoInfo info, bool isPartialEncode)
        {
            VideoJob vJob = new VideoJob(info, profile.VideoFormat);
            vJob.SetUpdate(task);

            VideoSliceInfo sliceInfo = isPartialEncode ? info as VideoSliceInfo : null;

            vJob.Input = profile.InputScript;
            vJob.EncoderPath = profile.Encoder;
            vJob.EncodeParam = profile.EncoderParam;
            vJob.NumaNode = numaNode;
            vJob.IsPartialEncode = isPartialEncode;
            if (vJob.IsPartialEncode)
            {
                vJob.NumberOfFrames = sliceInfo.FrameRange.GetLength();
                vJob.FrameRange = sliceInfo.FrameRange;
                vJob.PartId = sliceInfo.PartId;
            }
            else
            {
                vJob.NumberOfFrames = task.NumberOfFrames;
                vJob.FrameRange = new SliceInfo(0, -1);
                vJob.PartId = -1;
            }

            if (profile.Config != null)
                vJob.VspipeArgs.AddRange(profile.Config.VspipeArgs);

            vJob.Output = profile.WorkingPathPrefix + (vJob.IsPartialEncode ? $"_part{vJob.PartId}" : "");
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
            if (vJob.Info.QpFile != null)
            {
                if (vJob.CodecString == "AV1")
                    vJob.EncodeParam += $" --force-key-frames \"{vJob.Info.QpFile}\"";
                else
                    vJob.EncodeParam += $" --qpfile \"{vJob.Info.QpFile}\"";
            }

            task.JobQueue.Enqueue(vJob);
            Logger.Info($"添加压制任务：numFrame: {vJob.NumberOfFrames}, frameRange: {vJob.FrameRange}, qpfile: {vJob.Info.QpFile}, output: {vJob.Output}");
            return vJob;
        }

        // 新建封装处理工作
        private void GenerateMuxJob(TaskDetail task, TaskProfile profile, string inputFile, SliceInfo frameRange, int partId)
        {
            MuxJob mJob = new MuxJob(MuxType.SingleVideo, profile.ContainerFormat);
            mJob.SetUpdate(task);

            mJob.IsPartialMux = (frameRange.end != -1);
            mJob.FrameRange = frameRange;
            mJob.PartId = partId;
            mJob.Input = inputFile;
            mJob.Output = profile.WorkingPathPrefix + $"_part{partId}.{mJob.CodecString.ToLower()}";

            task.JobQueue.Enqueue(mJob);
            Logger.Info($"添加Mux任务：muxType: {mJob.MuxType}, frameRange: {mJob.FrameRange}, input: {mJob.Input}, output: {mJob.Output}");
        }

        private void GenerateMuxJob(TaskDetail task, TaskProfile profile, VideoInfo info)
        {
            MuxJob mJob = new MuxJob(MuxType.AppendVideo, profile.ContainerFormat, info);
            mJob.SetUpdate(task);

            mJob.TimeCodeFile = info.TimeCodeFile;
            mJob.VideoSlices.AddRange(task.ReEncodeVideoSlices);
            mJob.Output = profile.WorkingPathPrefix + $"_all.{mJob.CodecString.ToLower()}";

            task.JobQueue.Enqueue(mJob);
            Logger.Info($"添加Mux任务：muxType: {mJob.MuxType}, timeCodeFile: {mJob.TimeCodeFile}, output: {mJob.Output}");
        }

        private void GenerateMuxJob(TaskDetail task, TaskProfile profile, string reEncodeOldFile)
        {
            MuxJob mJob = new MuxJob(MuxType.MergeOldRemux, profile.ContainerFormat);
            mJob.SetUpdate(task);

            mJob.TimeCodeFile = (task.MediaOutFile.VideoTrack.Info as VideoInfo).TimeCodeFile;
            mJob.ReEncodeOldFile = reEncodeOldFile;
            mJob.Input = task.MediaOutFile.VideoTrack.File.GetFullPath();
            mJob.Output = Path.Combine(Path.GetDirectoryName(profile.OutputPathPrefix), task.OutputFile);

            task.JobQueue.Enqueue(mJob);
            Logger.Info($"添加Mux任务：muxType: {mJob.MuxType}, timeCodeFile: {mJob.TimeCodeFile}, input: {mJob.Input}, output: {mJob.Output}");
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
                            QAACEncoder qaac = new QAACEncoder(aJob);
                            qaac.start();
                            qaac.waitForFinish();
                        }
                        else if (srcFmt != "FLAC" && aJob.CodecString == "AAC" && info.Lossy)
                        {
                            task.CurrentStatus = "音频转码中";
                            FFmpegPipeQAACEncoder qaac = new FFmpegPipeQAACEncoder(aJob);
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
                        task.CurrentStatus = vJob.IsPartialEncode ? $"Part {vJob.PartId + 1}/{task.ReEncodeVideoSlices.Count} 压制中" : "压制中";
                        task.ProgressValue = 0.0;
                        processor.start();
                        processor.waitForFinish();

                        if (!vJob.IsPartialEncode)
                        {
                            task.MediaOutFile.AddTrack(new VideoTrack(new OKEFile(vJob.Output), vJob.Info));
                        }

                        break;
                    }
                    case MuxJob mJob:
                    {
                        MkvmergeMuxer muxer;
                        switch (mJob.MuxType)
                        {
                            case MuxType.SingleVideo:
                            {
                                muxer = new SingleVideoMuxer(mJob);
                                task.CurrentStatus = $"Part {mJob.PartId + 1}/{task.ReEncodeVideoSlices.Count} 封装中";
                                break;
                            }
                            case MuxType.AppendVideo:
                            {
                                muxer = new AppendVideoMuxer(mJob);
                                task.CurrentStatus = "视频拼接中";
                                break;
                            }
                            case MuxType.MergeOldRemux:
                            {
                                muxer = new MergeOldRemuxer(mJob);
                                task.CurrentStatus = "最终封装中";
                                break;
                            }
                            default:
                                throw new Exception($"unknown mux type: {mJob.MuxType}");
                        }

                        // 开始封装
                        task.ProgressValue = 0.0;
                        muxer.start();
                        muxer.waitForFinish();

                        if (mJob.MuxType == MuxType.SingleVideo)
                        {
                            task.ReEncodeVideoSlices[mJob.PartId].File = new OKEFile(mJob.Output);
                        }
                        else if (mJob.MuxType == MuxType.AppendVideo)
                        {
                            task.MediaOutFile.AddTrack(new VideoTrack(new OKEFile(mJob.Output), mJob.Info));
                        }
                        else if (mJob.MuxType == MuxType.MergeOldRemux)
                        {
                            OKEFile outFile = new OKEFile(mJob.Output);
                            outFile.AddCRC32();
                            task.OutputFile = outFile.GetFileName();
                            task.BitRate = CommandlineVideoEncoder.HumanReadableFilesize(outFile.GetFileSize(), 2);
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


        // 根据旧成品的I帧序列，生成最终的切片序列
        private void CheckReEncodeSlice(TaskDetail task, TaskProfile profile, IFrameInfo iFrameInfo)
        {
            Logger.Debug("Raw ReEncode Slice: " + profile.Config.ReEncodeSliceArray.ToString());

            var numFrames = iFrameInfo[iFrameInfo.Count - 1];

            foreach (var s in profile.Config.ReEncodeSliceArray)
            {
                if (s.begin >= numFrames || s.end > numFrames)
                {
                    OKETaskException ex = new OKETaskException(Constants.reEncodeSliceErrorSmr);
                    ex.Data["SLICE_ILLEGAL"] = $"{s}";
                    ex.Data["NUM_FRAMES"] = $"{numFrames}";
                    throw ex;
                }
                if (s.end == -1)
                {
                    s.end = numFrames;
                }
                s.begin = iFrameInfo.FindNearestLeft(s.begin);
                s.end = iFrameInfo.FindNearestRight(s.end);
            }

            profile.Config.ReEncodeSliceArray = profile.Config.ReEncodeSliceArray.Merge();

            Logger.Debug("IFrame ReEncode Slice: " + profile.Config.ReEncodeSliceArray.ToString());
        }

        // 新建各个切片的压制和封装处理工作
        private void GenerateReEncodeJob(TaskDetail task, TaskProfile profile, int numaNode, VideoInfo info)
        {
            List<VideoSliceInfo> reEncodeInfoList = new List<VideoSliceInfo>();
            SliceInfo curr_s;
            long prev_end = 0;
            int partId = 0;

            // 生成各切片的信息
            for (int i = 0; i < profile.Config.ReEncodeSliceArray.Count; i++)
            {
                curr_s = profile.Config.ReEncodeSliceArray[i];
                if (i == 0)
                {
                    if (curr_s.begin != 0)
                        reEncodeInfoList.Add(new VideoSliceInfo(false, new SliceInfo(0, curr_s.begin), partId++, null, null, info));
                }
                else
                {
                    reEncodeInfoList.Add(new VideoSliceInfo(false, new SliceInfo(prev_end, curr_s.begin), partId++, null, null, info));
                }

                IFrameInfo newChapterSlice = null;
                string qpFileName = profile.WorkingPathPrefix + $"_part{partId}.qpf";
                string qpFile = null;
                if (info.ChapterIFrameInfo != null)
                {
                    SliceInfo index = info.ChapterIFrameInfo.FindInRangeIndex(curr_s);
                    if (index != null)
                    {
                        newChapterSlice = new IFrameInfo(info.ChapterIFrameInfo.GetRange((int)index.begin, (int)(index.GetLength() + 1)));
                        newChapterSlice = new IFrameInfo(newChapterSlice.Select(x => x -= curr_s.begin));
                        qpFile = GenerateQpFile(newChapterSlice, qpFileName, profile.VideoFormat);
                    }
                }
                reEncodeInfoList.Add(new VideoSliceInfo(true, curr_s, partId++, qpFile, newChapterSlice, info));
                prev_end = curr_s.end;
            }
            if (prev_end != task.NumberOfFrames)
            {
                reEncodeInfoList.Add(new VideoSliceInfo(false, new SliceInfo(prev_end, task.NumberOfFrames), partId++, null, null, info));
            }

            foreach (var reInfo in reEncodeInfoList)
            {
                Logger.Debug($"ReEncode Slice Info: PartId: {reInfo.PartId}, IsReEncode: {reInfo.IsReEncode}, FrameRange: {reInfo.FrameRange}, " +
                             $"qpfile: {reInfo.QpFile}, chapterIFrameInfo: {reInfo.ChapterIFrameInfo}");
            }

            // 各切片的压制和封装工作
            task.ReEncodeVideoSlices = new List<VideoSliceTrack>();
            foreach (var reInfo in reEncodeInfoList)
            {
                VideoJob vJob;
                if (reInfo.IsReEncode)
                {
                    vJob = GenerateVideoJob(task, profile, numaNode, reInfo, true);
                    GenerateMuxJob(task, profile, vJob.Output, new SliceInfo(0, -1), reInfo.PartId);
                }
                else
                {
                    GenerateMuxJob(task, profile, profile.Config.ReEncodeOldFile, reInfo.FrameRange, reInfo.PartId);
                }
                task.ReEncodeVideoSlices.Add(new VideoSliceTrack(null, reInfo));
            }

            // 拼接各切片的工作
            GenerateMuxJob(task, profile, info);
        }

    }
}
