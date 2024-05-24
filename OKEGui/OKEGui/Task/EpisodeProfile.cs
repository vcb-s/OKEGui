using System;
using System.Collections.Generic;
using OKEGui.Utils;

namespace OKEGui.Task
{
    public class EpisodeConfig : ICloneable
    {
        public List<string> VspipeArgs = new List<string>();

        // 用于 Re-Encode 功能
        public bool EnableReEncode = false;
        public bool ReExtractSource = false;
        public string ReEncodeOldFile;
        public SliceInfoArray ReEncodeSliceArray;

        public object Clone()
        {
            EpisodeConfig clone = MemberwiseClone() as EpisodeConfig;
            if (VspipeArgs != null)
            {
                clone.VspipeArgs = new List<string>();
                foreach (string arg in VspipeArgs)
                {
                    clone.VspipeArgs.Add(arg);
                }
            }
            if (ReEncodeSliceArray != null)
            {
                clone.ReEncodeSliceArray = new SliceInfoArray();
                foreach (var s in ReEncodeSliceArray)
                {
                    clone.ReEncodeSliceArray.Add(s);
                }
            }
            return clone;
        }

        public override string ToString()
        {
            string str = "EpisodeConfig{ ";
            str += "VspipeArgs: ";
            if (VspipeArgs == null)
            {
                str += "null";
            }
            else
            {
                str += "[" + string.Join(",", VspipeArgs) + "], ";
            }

            str += $"EnableReEncode: {EnableReEncode}, ";
            str += $"ReEncodeOldFile: {ReEncodeOldFile}, ";
            str += "ReEncodeSliceArray: ";
            if (ReEncodeSliceArray == null)
            {
                str += "null";
            }
            else
            {
                str += ReEncodeSliceArray.ToString();
            }
            str += " }";
            return str;
        }
    }
}
