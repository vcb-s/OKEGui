using System;
using System.IO;


namespace OKEGui
{
    public partial class EACDemuxer
    {
        public enum TrackCodec
        {
            Unknown,
            MPEG2,
            H264_AVC,
            RAW_PCM,
            DTSMA,
            TRUEHD_AC3,
            AC3,
            DTS,
            PGS,
            Chapter,
        }

        public class TrackInfo
        {
            public TrackCodec Codec;
            public int Index;
            public string Information;
            public string RawOutput;
            public string SourceFile;
            public TrackType Type;
            public bool SkipMuxing;
            public long fileSize;
            public double meanVolume;
            public double maxVolume;

            public string OutFileName
            {
                get
                {
                    var directory = Path.GetDirectoryName(SourceFile);
                    var baseName = Path.GetFileNameWithoutExtension(SourceFile);

                    return $"{Path.Combine(directory, baseName)}_{Index}{FileExtension}";
                }
            }

            public string FileExtension
            {
                get
                {
                    TrackCodec type = Codec;
                    return "." + s_eacOutputs.Find(val => val.Codec == type).FileExtension;
                }
            }

            public bool IsEmpty()
            {
                switch (Type)
                {
                    case TrackType.Audio:
                        return meanVolume < -70 && maxVolume < -30;
                    default:
                        return fileSize == 0;
                }
            }

            public bool IsDuplicate(in TrackInfo other)
            {
                if (Type != other.Type)
                {
                    return false;
                }
                switch (Type)
                {
                    case TrackType.Audio:
                        return Math.Abs(meanVolume - other.meanVolume) < 0.15 && Math.Abs(maxVolume - other.maxVolume) < 0.15;
                    default:
                        return fileSize == other.fileSize;
                }
            }

            public void MarkSkipping()
            {
                File.Move(OutFileName, Path.ChangeExtension(OutFileName, ".bak") + FileExtension);
                SkipMuxing = true;
            }
        }
    }
}
