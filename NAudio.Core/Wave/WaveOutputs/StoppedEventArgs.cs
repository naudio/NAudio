using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Stopped Event Args
    /// </summary>
    public class StoppedEventArgs : EventArgs
    {
        private readonly Exception exception;

        /// <summary>
        /// Initializes a new instance of StoppedEventArgs
        /// </summary>
        /// <param name="exception">An exception to report (null if no exception)</param>
        public StoppedEventArgs(Exception exception = null)
        {
            this.exception = exception;
        }

        /// <summary>
        /// An exception. Will be null if the playback or record operation stopped due to 
        /// the user requesting stop or reached the end of the input audio
        /// </summary>
        public Exception Exception => exception;
    }
}
