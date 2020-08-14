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

        private Regex rmsLevelRegex = new Regex(@"RMS level dB: (-?(?:\d+\.\d+|inf))");
        private Regex peakLevelRegex = new Regex(@"Peak level dB: (-?(?:\d+\.\d+|inf))");

        public FFmpegVolumeChecker(string inputFile)
        {
            executable = Constants.ffmpegPath;
            commandLine = $"-i \"{inputFile}\" -af astats=measure_perchannel=none -f null /dev/null";
        }

        public override void ProcessLine(string line, StreamType stream)
        {
            base.ProcessLine(line, stream);

            var rmsLevel = rmsLevelRegex.Match(line);
            if (rmsLevel.Success)
            {
                var success = double.TryParse(rmsLevel.Groups[1].Value, out double meanVolume);
                MeanVolume = success ? meanVolume : double.NegativeInfinity;
                return;
            }

            var peakLevel = peakLevelRegex.Match(line);
            if (peakLevel.Success)
            {
                var success = double.TryParse(peakLevel.Groups[1].Value, out double maxVolume);
                MaxVolume = success ? maxVolume : double.NegativeInfinity;
                SetFinish();
            }
        }
    }
}
