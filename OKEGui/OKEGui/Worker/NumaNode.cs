using OKEGui.Utils;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace OKEGui.Worker
{
    static class NumaNode
    {
        [DllImport("Kernel32.dll")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool GetNumaHighestNodeNumber([Out] out uint HighestNodeNumber);

        public static readonly int NumaCount;
        public static readonly int UsableCoreCount;
        static int CurrentNuma;

        static NumaNode()
        {
            if (Initializer.Config.noNuma)
            {
                CurrentNuma = 0;
                NumaCount = 1;
            }
            else
            {
                GetNumaHighestNodeNumber(out uint temp);
                CurrentNuma = (int)temp;
                NumaCount = CurrentNuma + 1;
            }
            UsableCoreCount = Environment.ProcessorCount;
        }

        public static int NextNuma()
        {
            int res = CurrentNuma;
            CurrentNuma = (CurrentNuma - 1 + NumaCount) % NumaCount;
            return res;
        }

        public static int PrevNuma()
        {
            int res = CurrentNuma;
            CurrentNuma = (CurrentNuma + 1) % NumaCount;
            return res;
        }

        public static string X265PoolsParam(int currentNuma)
        {
            string[] res = Enumerable.Repeat("-", NumaCount).ToArray();
            res[currentNuma] = "+";
            return string.Join(",", res);
        }
    }
}
