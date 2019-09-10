using System;
using System.Collections.Generic;

namespace OKEGui.Task
{
    public class EpisodeConfig : ICloneable
    {
        public List<string> VspipeArgs = new List<string>();

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
            return clone;
        }

        public override string ToString()
        {
            string str = "EpisodeConfig{";
            str += "VspipeArgs: ";
            if (VspipeArgs == null)
            {
                str += "null";
            }
            else
            {
                str += "[" + string.Join(",", VspipeArgs) + "]";
            }
            str += "]";
            return str;
        }
    }
}
