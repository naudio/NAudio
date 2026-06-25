namespace NAudio.Wave;

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
