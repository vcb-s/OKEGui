using System;

namespace OKEGui
{
    /// <summary>
    /// Class that is used to send an encoding update from the encoder to the GUI
    /// it contains all the elements that will be updated in the GUI at some point
    /// code form megui
    /// </summary>
    public class StatusUpdate
    {
        private bool hasError, isComplete, wasAborted;
        private string error, log, jobName, status;
        private TimeSpan? audioPosition, cliplength, estimatedTime;
        private ulong? nbFramesDone, nbFramesTotal;
        private TimeSpan timeElapsed;
        private ulong? filesize, audioFileSize, projectedFileSize;
        private string processingspeed;
        private decimal percentage;

        internal StatusUpdate(string name)
        {
            jobName = name;

            estimatedTime = null;
            hasError = false;
            isComplete = false;
            wasAborted = false;
            error = null;
            log = null;
            audioPosition = null;
            cliplength = null;
            audioFileSize = null;
            nbFramesDone = null;
            nbFramesTotal = null;
            projectedFileSize = null;
            timeElapsed = TimeSpan.Zero;
            processingspeed = null;
            filesize = null;

            for (int i = 0; i < UpdatesPerEstimate; ++i) {
                previousUpdates[i] = TimeSpan.Zero;
                previousUpdatesProgress[i] = 0M;
            }
        }

        /// <summary>
        /// What is currently processing?
        /// </summary>
        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        /// <summary>
        /// Does the job have any Log?
        /// </summary>
        public string Log
        {
            get { return log; }
            set { log = value; }
        }

        /// <summary>
        /// What is the error ?
        /// </summary>
        public string Error
        {
            get { return error; }
            set { error = value; }
        }

        /// <summary>
        /// does the job have any errors?
        /// </summary>
        public bool HasError
        {
            get { return hasError; }
            set { hasError = value; }
        }

        /// <summary>
        ///  has the encoding job completed?
        /// </summary>
        public bool IsComplete
        {
            get { return isComplete; }
            set { isComplete = value; }
        }

        /// <summary>
        /// did we get this statusupdate because the job was aborted?
        /// </summary>
        public bool WasAborted
        {
            get { return wasAborted; }
            set { wasAborted = value; }
        }

        /// <summary>
        /// name of the job this statusupdate is refering to
        /// </summary>
        public string JobName
        {
            get { return jobName; }
            set { jobName = value; }
        }

        /// <summary>
        ///  position in clip
        /// </summary>
        public TimeSpan? ClipPosition
        {
            get { return audioPosition; }
            set { _currentTime = value ?? _currentTime; audioPosition = _currentTime; }
        }

        /// <summary>
        /// Length of clip
        /// </summary>
        public TimeSpan? ClipLength
        {
            get { return cliplength; }
            set { _totalTime = value ?? _totalTime; cliplength = _totalTime; }
        }

        /// <summary>
        /// number of frames that have been encoded so far
        /// </summary>
        public ulong? NbFramesDone
        {
            get { return nbFramesDone; }
            set { _frame = value ?? _frame; nbFramesDone = _frame; }
        }

        /// <summary>
        /// number of frames of the source
        /// </summary>
        public ulong? NbFramesTotal
        {
            get { return nbFramesTotal; }
            set { _framecount = value ?? _framecount; nbFramesTotal = _framecount; }
        }

        /// <summary>
        ///  current encoding speed
        /// </summary>
        public decimal? FPS
        {
            //			get {return fps;}
            set { _fps = value ?? _fps; }
        }

        /// <summary>
        /// Some estimate of the encoding speed (eg FPS, or ratio to realtime)
        /// </summary>
        public string ProcessingSpeed
        {
            get { return processingspeed; }
        }

        /// <summary>
        /// projected output size
        /// </summary>
        public ulong? ProjectedFileSize
        {
            get { return projectedFileSize; }
            set { _totalSize = value ?? _totalSize; projectedFileSize = _totalSize; }
        }

        public int PercentageDone
        {
            get { return (int)PercentageDoneExact; }
        }

        /// <summary>
        /// gets / sets the exact percentage of the encoding progress
        /// </summary>
        public decimal? PercentageDoneExact
        {
            get { return percentage; }
            set { _percent = value ?? _percent; percentage = _percent ?? 0M; }
        }

        /// <summary>
        /// size of the encoded file at this point
        /// </summary>
        public ulong? CurrentFileSize
        {
            get { return filesize; }
            set { _currentSize = value ?? _currentSize; filesize = _currentSize; }
        }

        /// <summary>
        /// current size of the audio
        /// this field is filled when muxing and contains the current size of the audio data
        /// </summary>
        public ulong? AudioFileSize
        {
            set { audioFileSize = value; }
        }

        /// <summary>
        /// time elapsed between start of encoding and the point where this status update is being sent
        /// </summary>
        public TimeSpan TimeElapsed
        {
            get { return timeElapsed; }
            set { timeElapsed = value; }
        }

        /// <summary>
        /// gets the elapsed time as a pretty string
        /// </summary>
        public string TimeElapsedString
        {
            get { return new TimeSpan(TimeElapsed.Hours, TimeElapsed.Minutes, TimeElapsed.Seconds).ToString(); }
        }

        /// <summary>
        /// Gets/sets the estimated time for this encode
        /// </summary>
        public TimeSpan? EstimatedTime
        {
            get { return estimatedTime; }
            set { _timeEstimate = value ?? _timeEstimate; estimatedTime = _timeEstimate; }
        }

        #region REAL variables

        private TimeSpan? _timeEstimate = null;
        public ulong? _audioSize = null;
        private decimal? _fps = null;

        // The following groups each allow progress to be calculated (in percent)
        private decimal? _percent = null;

        private ulong? _frame = null;
        private ulong? _framecount = null;

        private ulong? _currentSize = null;
        private ulong? _totalSize = null;

        private TimeSpan? _currentTime = null;
        private TimeSpan? _totalTime = null;

        #endregion REAL variables

        public void FillValues()
        {
            try {
                // First we attempt to find the percent done
                decimal? fraction = null;

                // Percent
                if (_percent.HasValue)
                    fraction = _percent / 100M;
                // Time estimates
                else if (_timeEstimate.HasValue && _timeEstimate != TimeSpan.Zero)
                    fraction = ((decimal)timeElapsed.Ticks / (decimal)_timeEstimate.Value.Ticks);
                // Frame counts
                else if (_frame.HasValue && _framecount.HasValue && _framecount != 0)
                    fraction = ((decimal)_frame.Value / (decimal)_framecount.Value);
                // File sizes
                else if (_currentSize.HasValue && _totalSize.HasValue && _totalSize != 0)
                    fraction = (_currentSize.Value / _totalSize.Value);
                // Clip positions
                else if (_currentTime.HasValue && _totalTime.HasValue && _totalTime != TimeSpan.Zero)
                    fraction = ((decimal)_currentTime.Value.Ticks / (decimal)_totalTime.Value.Ticks);

                if (fraction.HasValue) percentage = fraction.Value * 100M;

                /// Frame counts
                if (_frame.HasValue)
                    nbFramesDone = _frame.Value;
                if (_framecount.HasValue)
                    nbFramesTotal = _framecount.Value;
                if (_framecount.HasValue && !_frame.HasValue && fraction.HasValue)
                    nbFramesDone = (ulong)(fraction.Value * _framecount.Value);
                if (!_framecount.HasValue && _frame.HasValue && fraction.HasValue)
                    nbFramesTotal = (ulong)(_frame.Value / fraction.Value);

                /// Sizes
                if (_currentSize.HasValue)
                    filesize = _currentSize;
                if (_totalSize.HasValue)
                    projectedFileSize = _totalSize;
                if (_currentSize.HasValue && !_totalSize.HasValue && fraction.HasValue)
                    projectedFileSize = (ulong)(_currentSize / fraction.Value);
                // We don't estimate current size
                // because it would suggest to the user that
                // we are actually measuring it

                // We don't estimate the current time or total time
                // in the clip, because it would suggest we are measuring it.
                if (_currentTime.HasValue)
                    audioPosition = _currentTime;
                if (_totalTime.HasValue)
                    cliplength = _totalTime;
                // However, if we know the total time and the percent, it is
                // ok to estimate the current position
                if (_totalTime.HasValue && !_currentTime.HasValue && fraction.HasValue)
                    audioPosition = new TimeSpan((long)((decimal)_totalTime.Value.Ticks * fraction.Value));

                // FPS
                if (_frame.HasValue && timeElapsed.TotalSeconds > 0)
                    processingspeed =
                        ((decimal)_frame.Value / (decimal)timeElapsed.TotalSeconds).ToString("0") + " FPS";
                // Other processing speeds
                else if (_currentTime.HasValue && timeElapsed.Ticks > 0)
                    processingspeed =
                        ((decimal)_currentTime.Value.Ticks / (decimal)timeElapsed.Ticks).ToString("0") + "x realtime";
                else if (fraction.HasValue && _totalTime.HasValue && timeElapsed.Ticks > 0)
                    processingspeed =
                        ((decimal)_totalTime.Value.Ticks * fraction.Value / (decimal)timeElapsed.Ticks).ToString("0") + "x realtime";

                // Processing time
                if (fraction.HasValue) {
                    TimeSpan time = timeElapsed - previousUpdates[updateIndex];
                    decimal progress = fraction.Value - previousUpdatesProgress[updateIndex];
                    if (progress > 0 && time > FiveSeconds)
                        estimatedTime = new TimeSpan((long)((decimal)time.Ticks * (1M - fraction) / progress));
                    else
                        estimatedTime = new TimeSpan((long)((decimal)timeElapsed.Ticks * ((1 / fraction.Value) - 1)));

                    previousUpdates[updateIndex] = timeElapsed;
                    previousUpdatesProgress[updateIndex] = fraction.Value;
                    updateIndex = (updateIndex + 1) % UpdatesPerEstimate;
                }
            } catch (Exception) {
            }
        }

        private static readonly TimeSpan FiveSeconds = new TimeSpan(0, 0, 5);
        private const int UpdatesPerEstimate = 10;
        private TimeSpan[] previousUpdates = new TimeSpan[UpdatesPerEstimate];
        private decimal[] previousUpdatesProgress = new decimal[UpdatesPerEstimate];
        private int updateIndex = 0;
    }
}
