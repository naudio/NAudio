// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

using System.Runtime.Versioning;

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// AudioQueue parameter IDs <br />
/// Constants that identify the parameters for audio queues.
///
///	You can set a parameter in one of two ways:
///
///	- Assign a parameter value to an audio queue to take effect immediately using
///			AudioQueueSetParameter.
///	- Schedule a parameter to take effect when a buffer is enqueued. You supply the
///			parameter when you queue a buffer, and the new value of the parameter is applied
///			when that buffer is rendered.
///
///	AudioQueueGetParameter always returns the current value of the parameter
/// </summary>
internal enum AudioQueueParameterID : uint
{
    /// <summary>
    /// A value from 0.0 to 1.0 indicating the linearly scaled gain for the queue. A value of
    /// 1.0 (the default) indicates unity gain. A value of 0.0 indicates zero gain, or silence.
    /// </summary>
    kAudioQueueParam_Volume         = 1,
    /// <summary>
    /// A value from 0.5 to 2.0 indicating the rate at which the queue is to play. A value of
    /// 1.0 (the default) indicates that the queue should play at its normal rate. Only
    ///  applicable when the time/pitch processor has been enabled and on macOS 10.6 and higher.
    /// </summary>
    [SupportedOSPlatform("macos10.6")]
    kAudioQueueParam_PlayRate       = 2,
    /// <summary>
    /// A value from -2400 to 2400 indicating the number of cents to pitch-shift the queue's
    /// playback. (1200 cents is one octave.) Only applicable when the time/pitch processor has 
    /// been enabled with the spectral algorithm, and on macOS 10.6 and higher.
    /// </summary>
    [SupportedOSPlatform("macos10.6")]
    kAudioQueueParam_Pitch          = 3,
    /// <summary>
    /// A value indicating the number of seconds over which subsequent volume changes will be
    /// ramped. For example, to fade out from full unity gain to silence over the course of 1
    /// second, set kAudioQueueParam_VolumeRampTime to 1 then kAudioQueueParam_Volume to 0.
    /// </summary>
    kAudioQueueParam_VolumeRampTime = 4,
    /// <summary>
    /// A value from -1 to 1 indicating the pan position of a mono source (-1 = hard left, 0 =
    /// center, 1 = hard right). For a stereo source this parameter affects left/right balance.
    /// For multi-channel sources, this parameter has no effect.
    /// </summary>
    kAudioQueueParam_Pan            = 13
}