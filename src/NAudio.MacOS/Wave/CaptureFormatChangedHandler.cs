
namespace NAudio.Wave;

/// <summary>
/// Provides a delegate that is invoked when the capture format of the specified Core Audio recorder changes.
/// </summary>
/// <param name="theRecorder">The <see cref="CoreAudioRecorder"/> instance whose capture format was changed.</param>
public delegate void CaptureFormatChangedHandler(CoreAudioRecorder theRecorder);