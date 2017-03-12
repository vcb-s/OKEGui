using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKEGui
{
    public interface IMediaMuxer
    {
        /// <summary>
        /// 封装文件
        /// </summary>
        /// <param name="path">文件名</param>
        /// <param name="mediaFile">松散媒体文件</param>
        /// <returns>操作成功返回封装后的文件；失败返回null。</returns>
        IFile StartMuxing(string path, MediaFile mediaFile);

        /// <summary>
        /// 检查当前封装格式是否支持
        /// </summary>
        /// <param name="mediaFile">需要检查的文件结构</param>
        /// <returns></returns>
        bool IsCompatible(MediaFile mediaFile);

        // IMediaFile LoadFile(string path);
    }
}
