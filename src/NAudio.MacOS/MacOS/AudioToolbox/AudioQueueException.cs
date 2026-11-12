
using System;

using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the class for all the exceptions originating from the Audio Queue API.
/// </summary>
public class AudioQueueException : AudioToolboxException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="AudioQueueException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <param name="osStatus">The status code.</param>
    public AudioQueueException(int osStatus) : base(osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="AudioQueueException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public AudioQueueException(string? message, int osStatus) : base(message, osStatus) { }

    /// <summary>
    /// Throws an appropriate exception for the specified status code. <br />
    /// Does not throw if the specified code is not an error
    /// </summary>
    /// <param name="osStatus">The OS status code to throw an exception for</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="TimeoutException"></exception>
    /// <exception cref="AudioQueueException"></exception>
    public static void ThrowIfError(int osStatus)
    {
        if (osStatus == 0) { return; }

        if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidBuffer)
        {
            throw new ArgumentException("The specified buffer does not belong to the audio queue.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_BufferEmpty)
        {
            throw new ArgumentException("The buffer is empty (that is, the mAudioDataByteSize field = 0).");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_DisposalPending)
        {
            throw new ObjectDisposedException("The function cannot act on the audio queue because it is being asynchronously disposed of.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidProperty)
        {
            throw new ArgumentException("The specified property ID is invalid.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidPropertySize)
        {
            throw new ArgumentException("The size of the specified property is invalid.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidParameter)
        {
            throw new ArgumentException("The specified parameter ID is invalid.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_CannotStart)
        {
            throw new InvalidOperationException("The audio queue has encountered a problem and cannot start.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidDevice)
        {
            throw new ArgumentException("The device assigned to the queue could not be located, or is not properly configured.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_BufferInQueue)
        {
            throw new InvalidOperationException("The buffer cannot be disposed of when it is enqueued.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidRunState)
        {
            throw new InvalidOperationException("The queue is running but the function can only operate on the queue when it is stopped, or vice versa.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidQueueType)
        {
            throw new NotSupportedException("The queue is an input queue but the function can only operate on an output queue, or vice versa.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_Permissions)
        {
            throw new UnauthorizedAccessException("You do not have the required permissions to call the function.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidPropertyValue)
        {
            throw new ArgumentException("The specified property value is invalid.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_PrimeTimedOut)
        {
            throw new TimeoutException("During Prime, the queue's AudioConverter failed to convert the requested number of sample frames.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_CodecNotFound)
        {
            throw new NotSupportedException("The required audio codec was not found.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidCodecAccess)
        {
            throw new UnauthorizedAccessException("Access to the required codec is not permitted (possibly due to incompatible AudioSession settings on iOS).");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_QueueInvalidated)
        {
            throw new InvalidOperationException("The audio server has exited, causing this audio queue to have become invalid.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_TooManyTaps)
        {
            throw new InvalidOperationException("There can only be one processing tap per audio queue.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidTapContext)
        {
            throw new ArgumentException("GetTapSourceAudio can only be called from the tap's callback.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_RecordUnderrun)
        {
            throw new InvalidOperationException("During recording, data was lost because there was no enqueued buffer into which to store it.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidTapType)
        {
            throw new InvalidOperationException("GetTapQueueTime can only be called on an output queue's tap.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_BufferEnqueuedTwice)
        {
            throw new InvalidOperationException("A buffer was enqueued twice on an input queue (before being returned as a result of being filled or from Reset).");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_CannotStartYet)
        {
            throw new InvalidOperationException("Starting the audio queue failed because an internal reconfiguration (typically initiated by a hardware stream format or sample rate change) was in progress. Sleeping briefly and retrying is recommended.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_EnqueueDuringReset)
        {
            throw new InvalidOperationException("During Reset, Stop, or Dispose, it is not permitted to enqueue buffers.");
        }
        else if (osStatus == AudioQueueErrors.kAudioQueueErr_InvalidOfflineMode)
        {
            throw new NotSupportedException("The operation requires the queue to be in offline mode but it isn't, or vice versa. (Offline mode is entered and exited via AudioQueueSetOfflineRenderFormat).");
        }
        else
        {
            throw new AudioQueueException("Unknown error occurred.", osStatus);
        }
    }
}