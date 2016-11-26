namespace OKEGui
{
    public delegate void JobProcessingStatusUpdateCallback(StatusUpdate su);

    /// <summary>
    /// 任务处理。可执行单元
    /// </summary>
    public interface IJobProcessor
    {
        /// <summary>
        /// sets up encoding
        /// </summary
        /// <param name="job">the job to be processed</param>
        void setup(Job job, StatusUpdate su);

        /// <summary>
        /// starts the encoding process
        /// </summary>
        void start();

        /// <summary>
        /// stops the encoding process
        /// </summary>
        void stop();

        /// <summary>
        /// pauses the encoding process
        /// </summary>
        void pause();

        /// <summary>
        /// resumes the encoding process
        /// </summary>
        void resume();

        /// <summary>
        /// wait until job is finished
        /// </summary>
        void waitForFinish();

        /// <summary>
        /// changes the priority of the encoding process/thread
        /// </summary>
        void changePriority(ProcessPriority priority);

        event JobProcessingStatusUpdateCallback StatusUpdate;
    }
}
