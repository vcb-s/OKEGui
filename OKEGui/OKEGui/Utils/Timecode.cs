using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OKEGui.Utils
{
    public enum TimecodeVersion
    {
        V1,
        V2
    }

    internal class RangeInterval
    {
        public int StartFrame;
        public int EndFrame;
        public double Interval;

        public RangeInterval(int startFrame, int endFrame, double interval)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
            Interval = interval;
        }
    }

    public class Timecode
    {
        private readonly List<RangeInterval> _intervalList = new List<RangeInterval>();

        /// <summary>
        /// Total frames of the timecode file
        /// </summary>
        public TimeSpan TotalLength =>
            new TimeSpan((long) _intervalList.Sum(interval =>
                interval.Interval * (interval.EndFrame - interval.StartFrame + 1)));

        /// <summary>
        /// Total frames of the timecode file
        /// </summary>
        public int TotalFrames => _intervalList.Count == 0 ? 0 : _intervalList.Last().EndFrame + 1;

        /// <summary>
        /// Average frame rate of the timecode file
        /// </summary>
        public double AverageFrameRate => 1e7 * TotalFrames / TotalLength.Ticks;

        /// <summary>
        /// The constructor. It reads timecode info from specific file
        /// </summary>
        /// <param name="path">The path of the timecode file being loaded. It can either be in v1 or v2 format</param>
        /// <param name="frames">The total frames of the timecode file for fill the "gaps".
        /// This parameter only effected when reading timecode v1. If not provided, the last frame from the last record is used</param>
        public Timecode(string path, int frames = 0)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string line = reader.ReadLine() ?? throw new InvalidOperationException();
                    Regex regex = new Regex(@"\A# time(?:code|stamp) format (v[1-2])");
                    Match match = regex.Match(line);
                    if (!match.Success)
                    {
                        throw new FileFormatException("Can not determine the version of timecode file");
                    }

                    switch (match.Groups[1].Value)
                    {
                        case "v1":
                            TimecodeV1Handler(reader, frames);
                            break;
                        case "v2":
                            TimecodeV2Handler(reader);
                            break;
                    }
                }
            }

            NormalizeInterval();
        }

        /// <summary>
        /// Function to the timecode file.
        /// </summary>
        /// <param name="path">The location for timecode file being generated</param>
        /// <param name="version">The version of timecode file</param>
        public void SaveTimecode(string path, TimecodeVersion version = TimecodeVersion.V2)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    switch (version)
                    {
                        case TimecodeVersion.V1:
                            SaveTimecodeV1(writer);
                            break;
                        case TimecodeVersion.V2:
                            SaveTimecodeV2(writer);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(version), version, null);
                    }
                }
            }
        }

        /// <summary>
        /// Get frame number from a given time span
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public int GetFrameNumberFromTimeSpan(TimeSpan ts)
        {
            double time = 0;
            long tick = ts.Ticks;
            foreach (var interval in _intervalList)
            {
                time += interval.Interval * (interval.EndFrame - interval.StartFrame + 1);
                if (time < tick) continue;
                int deltaFrame = (int) Math.Round((time - tick) / interval.Interval);
                return interval.EndFrame - deltaFrame + 1;
            }

            return _intervalList.Last().EndFrame;
        }

        /// <summary>
        /// Get time span from a given frame number
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public TimeSpan GetTimeSpanFromFrameNumber(int frame)
        {
            double time = 0;
            foreach (var interval in _intervalList)
            {
                time += interval.Interval * (interval.EndFrame - interval.StartFrame + 1);
                if (interval.EndFrame < frame) continue;
                double deltaTime = interval.Interval * (interval.EndFrame - frame + 1);
                return new TimeSpan((long) Math.Round(time - deltaTime));
            }

            return new TimeSpan((long) Math.Round(time));
        }

        private void TimecodeV1Handler(TextReader reader, int frames)
        {
            double defaultInterval = 0;
            List<RangeInterval> intervals = new List<RangeInterval>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (!line.StartsWith("assume", true, null)) continue;
                defaultInterval = 1e7 / double.Parse(line.Substring(6));
                break;
            }

            if (defaultInterval == 0) throw new FileFormatException("Default FPS can not be found!");

            Regex lineRegex = new Regex(@"^(?<start>\d+)\s*,\s*(?<end>\d+)\s*,\s*(?<rate>\d+(?:\.\d*)?)$");
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                Match match = lineRegex.Match(line);
                if (!match.Success) throw new FileFormatException($"The content line is mis-formatted!\n{line}");
                int start = int.Parse(match.Groups["start"].Value);
                int end = int.Parse(match.Groups["end"].Value);
                double inter = 1e7 / double.Parse(match.Groups["rate"].Value);
                if (start > end)
                    throw new FileFormatException($"The start frame {start} is greater than the end frame {end}!");
                intervals.Add(new RangeInterval(start, end, inter));
            }

            intervals.Sort((a, b) => a.StartFrame - b.StartFrame);
            int lastEnd = -1;
            foreach (var interval in intervals)
            {
                if (interval.StartFrame <= lastEnd)
                {
                    _intervalList.Clear();
                    throw new FileFormatException(
                        $"Frame overlapped: -{lastEnd} and {interval.StartFrame}-{interval.EndFrame}"
                    );
                }

                if (interval.StartFrame - lastEnd > 1)
                {
                    _intervalList.Add(new RangeInterval(lastEnd + 1,
                        interval.StartFrame - 1, defaultInterval));
                }

                _intervalList.Add(interval);

                lastEnd = interval.EndFrame;
            }

            if (frames > TotalFrames)
            {
                _intervalList.Add(new RangeInterval(TotalFrames, frames - 1, defaultInterval));
            }
        }

        private void TimecodeV2Handler(TextReader reader)
        {
            double currentTime;
            double lastTime = -1;
            double lastDiff = 0;
            double firstTime = 0;
            int firstFrame = 0;
            int currentFrame = -1;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                currentTime = double.Parse(line);
                ++currentFrame;
                if (lastTime == -1)
                {
                    lastTime = currentTime;
                    continue;
                }

                if (currentTime == lastTime)
                {
                    if (currentFrame != 1)
                        throw new FileFormatException(
                            $"Frame overlapped: frame {currentFrame - 1} and frame {currentFrame}");
                    lastTime = currentTime;
                    continue;
                }

                if (Math.Abs(currentTime - lastTime - lastDiff) < 1e-3 || lastDiff == 0)
                {
                    lastDiff = currentTime - lastTime;
                    lastTime = currentTime;
                    continue;
                }

                _intervalList.Add(new RangeInterval(firstFrame, currentFrame - 2,
                    1e4 * (lastTime - firstTime) / (currentFrame - firstFrame - 1)));
                firstFrame = currentFrame - 1;
                firstTime = lastTime;
                lastDiff = currentTime - lastTime;
                lastTime = currentTime;
            }

            _intervalList.Add(new RangeInterval(firstFrame, currentFrame,
                (1e4 * (lastTime - firstTime) / (currentFrame - firstFrame))));
        }

        private void NormalizeInterval()
        {
            foreach (var interval in _intervalList)
            {
                if (Math.Abs(Math.Round(1001e4 / interval.Interval) - 1001e4 / interval.Interval) < 1e-6)
                {
                    interval.Interval = 1001e4 / Math.Round(1001e4 / interval.Interval);
                }
                else if (Math.Abs(Math.Round(1000e4 / interval.Interval) - 1000e4 / interval.Interval) < 1e-6)
                {
                    interval.Interval = 1000e4 / Math.Round(1000e4 / interval.Interval);
                }
            }
        }

        private void SaveTimecodeV1(TextWriter writer)
        {
            writer.WriteLine("# timecode format v1");
            if (_intervalList.Count == 0) return;

            double modeInterval = _intervalList.GroupBy(i => i.Interval)
                .OrderByDescending(g => g.Count())
                .First().Key;
            writer.WriteLine($"Assume {1e7 / modeInterval:F6}");

            foreach (var interval in _intervalList.Where(interval => interval.Interval != modeInterval))
            {
                writer.WriteLine($"{interval.StartFrame},{interval.EndFrame},{1e7 / interval.Interval:F6}");
            }
        }

        private void SaveTimecodeV2(TextWriter writer)
        {
            int frame = 0;
            double time = 0;
            writer.WriteLine("# timecode format v2");
            foreach (var interval in _intervalList)
            {
                while (frame <= interval.EndFrame)
                {
                    writer.WriteLine((time / 1e4).ToString("F6"));
                    time += interval.Interval;
                    ++frame;
                }
            }
        }
    }
}
