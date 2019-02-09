using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui
{
    public enum TrackType
    {
        Audio,
        Subtitle,
        Unknown,
        Video,
        Chapter,
    }

    public class MediaTrack
    {
        /// <summary>
        /// 获取当前轨道的ID (每个轨道在一个文件内是唯一的)
        /// </summary>
        /// <remarks>目前没有什么用，主要是以在List的顺序为主</remarks>
        /// <returns>所在文件的轨道ID</returns>
        public int TrackId;

        /// <summary>
        /// 编码格式名称，如果可用。
        /// </summary>
        public string CodecName;

        /// <summary>
        /// 轨道类型
        /// </summary>
        public TrackType TrackType;

        /// <summary>
        /// 当前轨道是否被禁用
        /// </summary>
        public bool IsDisable = false;

        /// <summary>
        /// 当前轨道所在文件。如果在已封装文件里，可以转换为IMediaContainer接口。
        /// </summary>
        public IFile File;

    }

    public class VideoTrack : MediaTrack
    {
        public VideoInfo VideoInfo;

        public VideoTrack(IFile file, VideoInfo videoInfo)
        {
            TrackType = TrackType.Video;
            File = file;
            VideoInfo = videoInfo;
        }
    }

    public class AudioTrack : MediaTrack
    {
        public AudioInfo AudioInfo;

        public AudioTrack(IFile file, AudioInfo audioInfo)
        {
            TrackType = TrackType.Audio;
            File = file;
            AudioInfo = audioInfo;
        }
    }

    public class SubtitleTrack : MediaTrack
    {
        public string Language;

        public SubtitleTrack(IFile file, string language)
        {
            TrackType = TrackType.Subtitle;
            File = file;
            Language = language;
        }
    }

    public class ChapterTrack : MediaTrack
    {
        public ChapterTrack(IFile file)
        {
            TrackType = TrackType.Chapter;
            File = file;
        }
    }

    public class VideoInfo
    {
        public uint Width;
        public uint Height;
        public uint FrameCount;
        public uint FpsNum;
        public uint FpsDen;
        public double Fps;

        public VideoInfo()
        {
        }

        public VideoInfo(uint fpsNum, uint fpsDen)
        {
            this.FpsNum = fpsNum;
            this.FpsDen = fpsDen;

            this.Fps = fpsNum / fpsDen;
        }

        public VideoInfo(double fps) : this(0, 0, 0, fps)
        {
        }

        public VideoInfo(uint width, uint height,
            uint framecount, double fps)
        {
            Width = width;
            Height = height;
            FrameCount = framecount;
            uint fps1000 = (uint)(fps * 1000 + 0.5);
            switch (fps1000)
            {
                case 23976:
                    FpsNum = 24000;
                    FpsDen = 1001;
                    break;

                case 29970:
                    FpsNum = 30000;
                    FpsDen = 1001;
                    break;

                case 47952:
                    FpsNum = 48000;
                    FpsDen = 1001;
                    break;

                case 59940:
                    FpsNum = 60000;
                    FpsDen = 1001;
                    break;

                default:
                    FpsNum = fps1000;
                    FpsDen = 1000;
                    break;
            }
        }
    }

    // 音轨信息
    public class AudioInfo
    {
        public int TrackId { get; set; }
        public string SourceCodec { get; set; }
        public string OutputCodec { get; set; }
        public int Bitrate { get; set; }
        public string Language { get; set; }
        public string ExtraArg { get; set; }
        public bool SkipMuxing { get; set; }
    }

    /// <summary>
    /// 松散媒体文件结构类
    /// 程序内部抽象媒体结构，包含的轨道一般为未封装文件；
    /// 需要调用IMediaContainer封装保存到硬盘里。
    /// </summary>
    /// <remarks>最终轨道ID顺序：视频轨->音频轨-></remarks>
    public class MediaFile
    {
        /// <summary>
        /// 返回所有轨道
        /// </summary>
        /// <remarks>通过此变量删除轨道不会产生影响。</remarks>
        public List<MediaTrack> Tracks
        {
            get {
                List<MediaTrack> tracks = new List<MediaTrack>();
                if (VideoTrack != null)
                {
                    tracks.Add(VideoTrack);
                }

                tracks.AddRange(AudioTracks);
                tracks.AddRange(SubtitleTracks);

                if (Chapter != null)
                {
                    tracks.Add(Chapter);
                }

                return tracks;
            }
        }

        public VideoTrack VideoTrack = null;
        public List<AudioTrack> AudioTracks = new List<AudioTrack>();
        public List<SubtitleTrack> SubtitleTracks = new List<SubtitleTrack>();

        public ChapterTrack Chapter = null;

        /// <summary>
        /// 插入多媒体轨道
        /// </summary>
        /// <param name="track">待添加轨道</param>
        /// <remarks>
        /// 一个多媒体文件只能有一条视频轨道和章节。如果已经存在，返回false。
        /// TrackId如果为0则添加到末尾；如果不为0，插入到所属轨道类型的第TrackId个之前(从1开始)。
        /// 视频轨道永远为第一条。
        /// </remarks>
        /// <returns>是否成功</returns>
        public bool AddTrack(MediaTrack track)
        {
            if (track.TrackType == TrackType.Video &&
                VideoTrack != null)
            {
                return false;
            }

            if (track.TrackType == TrackType.Chapter &&
                Chapter != null)
            {
                return false;
            }

            if (track is VideoTrack)
            {
                VideoTrack = track as VideoTrack;
            }

            if (track is AudioTrack)
            {
                if (track.TrackId != 0)
                {
                    AudioTracks.Insert(track.TrackId - 1, track as AudioTrack);
                }

                AudioTracks.Add(track as AudioTrack);
            }

            if (track is SubtitleTrack)
            {
                if (track.TrackId != 0)
                {
                    SubtitleTracks.Insert(track.TrackId - 1, track as SubtitleTrack);
                }
                SubtitleTracks.Add((SubtitleTrack)track);
            }

            if (track is ChapterTrack)
            {
                Chapter = track as ChapterTrack;
            }

            return true;
        }

        /// <summary>
        /// 删除轨道。并不会删除硬盘中的文件。
        /// </summary>
        /// <param name="op">操作函数。返回True</param>
        /// <returns>返回已经移除的轨道。</returns>
        public MediaTrack RemoveTrack(Func<MediaTrack, bool> op)
        {
            foreach (var track in Tracks)
            {
                if (op(track))
                {
                    if (track == VideoTrack)
                    {
                        VideoTrack = null;
                        return track;
                    }
                    else if (track == Chapter)
                    {
                        Chapter = null;
                        return track;
                    }
                    else if (track is AudioTrack)
                    {
                        if (AudioTracks.Remove((AudioTrack)track))
                        {
                            return track;
                        }
                    }
                    else if (track is SubtitleTrack)
                    {
                        if (SubtitleTracks.Remove((SubtitleTrack)track))
                        {
                            return track;
                        }
                    }
                }
            }

            return null;
        }
    }
}
