namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Provides the different types of work queues that can be created in Media Foundation. <br />
    /// Internally used at the moment.
    /// </summary>
    internal enum WorkQueueType : uint
    {
        /// <summary>
        /// Create a work queue without a message loop
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Create a work queue with a message loop.
        /// </summary>
        Window = 1,
        /// <summary>
        /// Create a multithreaded work queue. <br />
        /// This type of work queue uses a thread pool to dispatch work items.  <br />
        /// The caller is responsible for serializing the work items.
        /// </summary>
        MultiThreaded = 2
    }
}
