using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OKEGui.Model;

namespace OKEGui.Model
{
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
        public List<Track> Tracks
        {
            get
            {
                List<Track> tracks = new List<Track>();
                if (VideoTrack != null)
                {
                    tracks.Add(VideoTrack);
                }

                tracks.AddRange(AudioTracks);
                tracks.AddRange(SubtitleTracks);

                if (ChapterTrack != null)
                {
                    tracks.Add(ChapterTrack);
                }

                return tracks;
            }
        }

        public VideoTrack VideoTrack = null;
        public List<AudioTrack> AudioTracks = new List<AudioTrack>();
        public List<SubtitleTrack> SubtitleTracks = new List<SubtitleTrack>();

        public ChapterTrack ChapterTrack = null;
        public string ChapterLanguage = null;

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
        public void AddTrack(Track track)
        {
            if (track is VideoTrack)
            {
                if (VideoTrack == null)
                {
                    VideoTrack = track as VideoTrack;
                }
                else
                {
                    throw new ArgumentException("Multiple Video tracks are being added!");
                }
            }

            if (track is AudioTrack)
            {
                AudioTracks.Add(track as AudioTrack);
            }

            if (track is SubtitleTrack)
            {
                SubtitleTracks.Add(track as SubtitleTrack);
            }

            if (track is ChapterTrack)
            {
                if (ChapterTrack == null)
                {
                    ChapterTrack = track as ChapterTrack;
                }
                else
                {
                    throw new ArgumentException("Multiple Chapter tracks are being added!");
                }
            }
        }
    }
}
