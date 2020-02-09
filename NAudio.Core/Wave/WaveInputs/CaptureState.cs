// for consistency this should be in NAudio.Wave namespace, but left as it is for backwards compatibility
// ReSharper disable once CheckNamespace
namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Represents state of a capture device
    /// </summary>
    public enum CaptureState
    {
        /// <summary>
        /// Not recording
        /// </summary>
        Stopped,
        /// <summary>
        /// Beginning to record
        /// </summary>
        Starting,
        /// <summary>
        /// Recording in progress
        /// </summary>
        Capturing,
        /// <summary>
        /// Requesting stop
        /// </summary>
        Stopping
    }
}