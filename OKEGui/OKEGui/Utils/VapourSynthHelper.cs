using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

// using VSHelper;

namespace OKEGui
{
    public struct VSCoreInfo
    {
        public string versionString;
        public int core;
        public int api;
        public int numThreads;
        public long maxFramebufferSize;
        public long usedFramebufferSize;
    }

    public struct VSFormat
    {
        public string name;
        public int id;
        public int colorFamily; /* see VSColorFamily */
        public int sampleType; /* see VSSampleType */
        public int bitsPerSample; /* number of significant bits */
        public int bytesPerSample; /* actual storage is always in a power of 2 and the smallest possible that can fit the number of bits used per sample */

        public int subSamplingW; /* log2 subsampling factor, applied to second and third plane */
        public int subSamplingH;

        public int numPlanes; /* implicit from colorFamily */

        //extra
        public string colorFamilyName;
    }

    public struct VSVideoInfo
    {
        public VSFormat format;
        public long fpsNum;
        public long fpsDen;
        public int width;
        public int height;
        public int numFrames; /* api 3.2 - no longer allowed to be 0 */
        public int flags;

        // extra
        public double fps;
    }

    public class VapourSynthHelper
    {
        private int totalFrames;
        private long fpsNum;
        private long fpsDen;
        private int width;
        private int height;
        private bool isInit;
        private VSVideoInfo videoInfo;

        private int vsHandle;
        // private CVSHelper vss;

        [DllImport("VSWarpper.dll", CharSet = CharSet.Unicode)]
        private static extern int InitVSLibrary(string dllPath);

        [DllImport("VSWarpper.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsVSLibraryInit(int vsLib);

        [DllImport("VSWarpper.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LoadScript(int vsLib, string script, string scriptName);

        [DllImport("VSWarpper.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LoadScriptFile(int vsLib, string scriptPath);

        [DllImport("VSWarpper.dll")]
        private static extern IntPtr GetVideoInfo(int vsLib);

        [DllImport("VSWarpper.dll")]
        private static extern void UnloadScript(int vsLib);

        [DllImport("VSWarpper.dll")]
        private static extern void CloseVSLibrary(int vsLib);

        public VapourSynthHelper()
        {
            vsHandle = InitVSLibrary(null);
            isInit = IsVSLibraryInit(vsHandle);
            // vss = new CVSHelper();
            // isInit = vss.IsInit();
        }

        public VapourSynthHelper(string vsdllPath)
        {
            vsHandle = InitVSLibrary(vsdllPath);
            isInit = IsVSLibraryInit(vsHandle);
            // vss = new CVSHelper(vsdllPath);
            // isInit = vss.IsInit();
        }

        public bool LoadScript(string src, string name)
        {
            bool result = LoadScript(vsHandle, src, name);
            UpdateVideoInfo();
            return result;
        }

        public bool LoadScriptFile(string path)
        {
            bool result = LoadScriptFile(vsHandle, path);
            UpdateVideoInfo();
            return result;
        }

        public void UpdateVideoInfo()
        {
            VSVideoInfo vinfo = VideoInfo;
            totalFrames = vinfo.numFrames;
            // fps = vinfo.g
            fpsNum = vinfo.fpsNum;
            fpsDen = vinfo.fpsDen;
            width = vinfo.width;
            height = vinfo.height;
        }

        private void UnloadScript()
        {
            UnloadScript(vsHandle);
        }

        #region Getter

        public int TotalFreams
        {
            get { return totalFrames; }
        }

        public long FpsNum
        {
            get { return fpsNum; }
        }

        public long FpsDen
        {
            get { return fpsDen; }
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public bool IsInit
        {
            get { return isInit; }
        }

        public VSVideoInfo VideoInfo
        {
            get {
                var vsvinfo = GetVideoInfo(vsHandle);
                videoInfo = new VSVideoInfo { };

                Byte[] vInfoBuf = new Byte[40];
                Byte[] buf = new Byte[64];

                Marshal.Copy(vsvinfo, vInfoBuf, 0, 40);

                //vsvinfo.Seek(0, SeekOrigin.Begin);
                //vsvinfo.Read(buf, 0, 40);

                Byte[] dataVSf = new Byte[64];
                Marshal.Copy(new IntPtr(BitConverter.ToInt64(vInfoBuf, 0)), dataVSf, 0, 64);

                videoInfo.format.name = System.Text.Encoding.UTF8.GetString(dataVSf.Take(32).ToArray());
                videoInfo.format.id = BitConverter.ToInt32(dataVSf, 32);
                videoInfo.format.colorFamily = BitConverter.ToInt32(dataVSf, 36);
                videoInfo.format.sampleType = BitConverter.ToInt32(dataVSf, 40);
                videoInfo.format.bitsPerSample = BitConverter.ToInt32(dataVSf, 44);
                videoInfo.format.bytesPerSample = BitConverter.ToInt32(dataVSf, 48);
                videoInfo.format.subSamplingW = BitConverter.ToInt32(dataVSf, 52);
                videoInfo.format.subSamplingH = BitConverter.ToInt32(dataVSf, 56);
                videoInfo.format.numPlanes = BitConverter.ToInt32(dataVSf, 60);

                videoInfo.fpsNum = BitConverter.ToInt64(vInfoBuf, 8);
                videoInfo.fpsDen = BitConverter.ToInt64(vInfoBuf, 16);
                videoInfo.width = BitConverter.ToInt32(vInfoBuf, 24);
                videoInfo.height = BitConverter.ToInt32(vInfoBuf, 28);
                videoInfo.numFrames = BitConverter.ToInt32(vInfoBuf, 32);
                videoInfo.flags = BitConverter.ToInt32(vInfoBuf, 36);

                return videoInfo;
            }
        }

        #endregion Getter
    }

    public class VSPipeInfo
    {
        private int totalFrames;
        private double fps;
        private long fpsNum;
        private long fpsDen;
        private int width;
        private int height;
        private VSVideoInfo videoInfo;

        public VSPipeInfo(VideoJob vjob)
        {
            VideoInfoJob j = new VideoInfoJob(vjob);

            VSPipeProcessor processor = new VSPipeProcessor(j);
            processor.start();

            videoInfo = processor.VideoInfo;
            UpdateVideoInfo();
        }

        public void UpdateVideoInfo()
        {
            totalFrames = videoInfo.numFrames;
            fps = videoInfo.fps;
            fpsNum = videoInfo.fpsNum;
            fpsDen = videoInfo.fpsDen;
            width = videoInfo.width;
            height = videoInfo.height;
        }

        public int TotalFreams
        {
            get { return totalFrames; }
        }

        public double Fps
        {
            get { return fps; }
        }

        public long FpsNum
        {
            get { return fpsNum; }
        }

        public long FpsDen
        {
            get { return fpsDen; }
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }
    }
}
