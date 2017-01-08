using System;
using System.Collections.Generic;
using System.IO;

namespace OKEGui
{
    class JobWorker
    {
        private Job job;

        public JobWorker(Job j)
        {
            job = j;
        }

        public void Start()
        {
            // TODO: 只考虑完整的视频压制流程
            if (job.JobType != "video") {
                return;
            }

            VideoJob vjob = job as VideoJob;

            // 抽取音轨
            FileInfo eacInfo = new FileInfo(".\\tools\\eac3to\\eac3to.exe");
            if (!eacInfo.Exists) {
                throw new Exception("Eac3to 不存在");
            }
            EACDemuxer eac = new EACDemuxer(eacInfo.FullName, vjob.config.InputFile);
            var audioTracks = eac.Extract((double progress, EACProgressType type) => {
                switch (type) {
                    case EACProgressType.Analyze:
                        vjob.config.Status = "轨道分析中";
                        vjob.config.ProgressValue = progress;
                        break;

                    case EACProgressType.Process:
                        vjob.config.Status = "抽取音轨中";
                        vjob.config.ProgressValue = progress;
                        break;

                    case EACProgressType.Completed:
                        vjob.config.Status = "音轨抽取完毕";
                        vjob.config.ProgressValue = progress;
                        break;

                    default:
                        return;
                }
            });

            // 默认列表是按照顺序来
            string audioTrack = audioTracks[0].OutFileName;

            // 音频转码
            List<string> audioFile = new List<string>();
            foreach (var track in vjob.config.AudioTracks) {
                var audioOutput = audioTracks[track.TrackId];
                string audioOutpath = audioOutput.OutFileName;

                if (track.Format == "AAC") {
                    vjob.config.Status = "音轨转码中";
                    vjob.config.ProgressValue = -1;

                    if (audioOutput.FileExtension == ".flac") {
                        AudioJob aDecode = new AudioJob("WAV");
                        aDecode.Input = audioOutput.OutFileName;
                        aDecode.Output = "-";
                        FLACDecoder flac = new FLACDecoder(".\\tools\\flac\\flac.exe", aDecode);

                        AudioJob aEncode = new AudioJob("AAC");
                        aEncode.Input = "-";
                        aEncode.Output = Path.ChangeExtension(audioOutpath, ".aac");
                        QAACEncoder qaac = new QAACEncoder(".\\tools\\qaac\\qaac.exe", aEncode, track.Bitrate);

                        CMDPipeJobProcessor cmdpipe = CMDPipeJobProcessor.NewCMDPipeJobProcessor(flac, qaac);
                        cmdpipe.start();
                        cmdpipe.waitForFinish();

                        audioOutpath = aEncode.Output;
                    }
                }

                var audioFileInfo = new FileInfo(audioOutpath);
                if (audioFileInfo.Length < 1024) {
                    // 无效音轨
                    // TODO: 提示用户不能封装
                    File.Move(audioOutpath, Path.ChangeExtension(audioOutpath, ".bak") + audioFileInfo.Extension);
                    continue;
                }

                if (track.IsMux) {
                    audioFile.Add(audioOutpath);
                }
            }

            vjob.config.Status = "获取信息中";
            IJobProcessor processor = x265Encoder.init(vjob, vjob.config.EncoderParam);

            vjob.config.Status = "压制中";
            vjob.config.ProgressValue = 0.0;
            processor.start();
            processor.waitForFinish();

            if (vjob.config.ContainerFormat != "") {
                // 封装
                vjob.config.Status = "封装中";
                FileInfo mkvInfo = new FileInfo(".\\tools\\mkvtoolnix\\mkvmerge.exe");
                if (!mkvInfo.Exists) {
                    throw new Exception("mkvmerge不存在");
                }

                FileInfo lsmash = new FileInfo(".\\tools\\l-smash\\muxer.exe");
                if (!lsmash.Exists) {
                    throw new Exception("l-smash 封装工具不存在");
                }

                AutoMuxer muxer = new AutoMuxer(mkvInfo.FullName, lsmash.FullName);
                muxer.ProgressChanged += progress => vjob.config.ProgressValue = progress;

                List<string> mergeList = new List<string> {
                    vjob.config.InputFile + ".hevc",
                    Path.ChangeExtension(vjob.config.InputFile, ".txt"),
                };
                mergeList.AddRange(audioFile);

                muxer.StartMerge(mergeList, vjob.Output);
            }

            vjob.config.Status = "完成";
            vjob.config.ProgressValue = 100;
        }
    }
}
