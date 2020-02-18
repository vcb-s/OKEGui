using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OKEGui.Utils;

namespace OKEGui
{
    class FFmpegVolumeChecker : CommandlineJobProcessor
    {
        public double MeanVolume { get; private set; }
        public double MaxVolume { get; private set; }

        public FFmpegVolumeChecker(string inputFile)
        {
            executable = Constants.ffmpegPath;
            commandLine = "-i \"" + inputFile + "\" -af astats=measure_perchannel=none -f null /dev/null";
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);

            if (line.Contains("RMS level dB"))
            {
                Regex rf = new Regex(@"RMS level dB: (-?\d+.\d+)");
                string[] result = rf.Split(line);
                MeanVolume = double.Parse(result[1]);
                return;
            }

            if (line.Contains("Peak level dB"))
            {
                Regex rf = new Regex(@"Peak level dB: (-?\d+.\d+)");
                string[] result = rf.Split(line);
                MaxVolume = double.Parse(result[1]);
                SetFinish();
            }
        }
    }
}
