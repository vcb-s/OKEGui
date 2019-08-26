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
            commandLine = "-i \"" + inputFile + "\" -af volumedetect -f null /dev/null";
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            if (line.Contains("mean_volume"))
            {
                line = line.Replace(": 0.", ": -0.");
                Regex rf = new Regex("mean_volume: (-[0-9]+.[0-9]+) dB");
                string[] result = rf.Split(line);
                MeanVolume = double.Parse(result[1]);
            }
            if (line.Contains("max_volume"))
            {
                line = line.Replace(": 0.", ": -0.");
                Regex rf = new Regex("max_volume: (-[0-9]+.[0-9]+) dB");
                string[] result = rf.Split(line);
                MaxVolume = double.Parse(result[1]);
                SetFinish();
            }
        }
    }
}
