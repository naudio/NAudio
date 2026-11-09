// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

namespace NAudio.MacOS.AudioToolbox.Interop;

/*
    @constant   kAudioQueueErr_InvalidCodecAccess
    				Access to the required codec is not permitted (possibly due to incompatible
    				AudioSession settings on iOS).
    @constant   kAudioQueueErr_QueueInvalidated
    				On iOS derived platforms (excluding Catalyst), the audio server has exited,
    				causing this audio queue to have become invalid.
    @constant   kAudioQueueErr_TooManyTaps
    				
    @constant   kAudioQueueErr_InvalidTapContext
    				GetTapSourceAudio can only be called from the tap's callback.
    @constant   kAudioQueueErr_InvalidTapType
    				
    @constant   kAudioQueueErr_RecordUnderrun
    				During recording, data was lost because there was no enqueued buffer into which
    				to store it.
    @constant   kAudioQueueErr_BufferEnqueuedTwice
    				A buffer was enqueued twice on an input queue (before being returned as a result
    				of being filled or from Reset).
    @constant   kAudioQueueErr_CannotStartYet
    				Starting the audio queue failed because an internal reconfiguration (typically
    				initiated by a hardware stream format or sample rate change) was in progress.
    				Sleeping briefly and retrying is recommended.
    @constant   kAudioQueueErr_EnqueueDuringReset
    				
    @constant   kAudioQueueErr_InvalidOfflineMode
    				The operation requires the queue to be in offline mode but it isn't, or vice
    				versa. (Offline mode is entered and exited via
    				AudioQueueSetOfflineRenderFormat).
*/

internal static class AudioQueueErrors
{
    /// <summary>
    /// The specified buffer does not belong to the audio queue.
    /// </summary>
    public const int kAudioQueueErr_InvalidBuffer        = -66687;
    /// <summary>
    /// The buffer is empty (that is, the mAudioDataByteSize field = 0).
    /// </summary>
    public const int kAudioQueueErr_BufferEmpty          = -66686;
    /// <summary>
    /// The function cannot act on the audio queue because it is being asynchronously disposed of.
    /// </summary>
    public const int kAudioQueueErr_DisposalPending      = -66685;
    /// <summary>
    /// The specified property ID is invalid.
    /// </summary>
    public const int kAudioQueueErr_InvalidProperty      = -66684;
    /// <summary>
    /// The size of the specified property is invalid.
    /// </summary>
    public const int kAudioQueueErr_InvalidPropertySize  = -66683;
    /// <summary>
    /// The specified parameter ID is invalid.
    /// </summary>
    public const int kAudioQueueErr_InvalidParameter     = -66682;
    /// <summary>
    /// The audio queue has encountered a problem and cannot start.
    /// </summary>
    public const int kAudioQueueErr_CannotStart          = -66681;
    /// <summary>
    /// The device assigned to the queue could not be located, or is not properly configured.
    /// </summary>
    public const int kAudioQueueErr_InvalidDevice        = -66680;
    /// <summary>
    /// The buffer cannot be disposed of when it is enqueued.
    /// </summary>
    public const int kAudioQueueErr_BufferInQueue        = -66679;
    /// <summary>
    /// The queue is running but the function can only operate on the queue when it is stopped, or vice versa.
    /// </summary>
    public const int kAudioQueueErr_InvalidRunState      = -66678;
    /// <summary>
    /// The queue is an input queue but the function can only operate on an output queue, or vice versa.
    /// </summary>
    public const int kAudioQueueErr_InvalidQueueType     = -66677;
    /// <summary>
    /// You do not have the required permissions to call the function
    /// </summary>
    public const int kAudioQueueErr_Permissions          = -66676;
    /// <summary>
    /// The specified property value is invalid.
    /// </summary>
    public const int kAudioQueueErr_InvalidPropertyValue = -66675;
    /// <summary>
    /// During Prime, the queue's AudioConverter failed 
    /// to convert the requested number of sample frames.
    /// </summary>
    public const int kAudioQueueErr_PrimeTimedOut        = -66674;
    /// <summary>
    /// The required audio codec was not found.
    /// </summary>
    public const int kAudioQueueErr_CodecNotFound        = -66673;
    /// <summary>
    /// Access to the required codec is not permitted 
    /// (possibly due to incompatible AudioSession settings on iOS).
    /// </summary>
    public const int kAudioQueueErr_InvalidCodecAccess   = -66672;
    /// <summary>
    /// On iOS derived platforms (excluding Catalyst), the audio server has exited,
    /// causing this audio queue to have become invalid.
    /// </summary>
    public const int kAudioQueueErr_QueueInvalidated     = -66671;
    /// <summary>
    /// There can only be one processing tap per audio queue.
    /// </summary>
    public const int kAudioQueueErr_TooManyTaps          = -66670;
    /// <summary>
    /// GetTapSourceAudio can only be called from the tap's callback.
    /// </summary>
    public const int kAudioQueueErr_InvalidTapContext    = -66669;
    /// <summary>
    /// GetTapQueueTime can only be called on an output queue's tap.
    /// </summary>
    public const int kAudioQueueErr_RecordUnderrun       = -66668;
    /// <summary>
    /// GetTapQueueTime can only be called on an output queue's tap.
    /// </summary>
    public const int kAudioQueueErr_InvalidTapType       = -66667;
    /// <summary>
    /// A buffer was enqueued twice on an input queue (before being returned as a result
    /// of being filled or from Reset).
    /// </summary>
    public const int kAudioQueueErr_BufferEnqueuedTwice  = -66666;
    /// <summary>
    /// Starting the audio queue failed because an internal reconfiguration (typically
    /// initiated by a hardware stream format or sample rate change) was in progress.
    /// Sleeping briefly and retrying is recommended.
    /// </summary>
    public const int kAudioQueueErr_CannotStartYet       = -66665;
    /// <summary>
    /// During Reset, Stop, or Dispose, it is not permitted to enqueue buffers.
    /// </summary>
    public const int kAudioQueueErr_EnqueueDuringReset   = -66632;
    /// <summary>
    /// The operation requires the queue to be in offline mode but it isn't, or vice
    /// versa. (Offline mode is entered and exited via
    /// AudioQueueSetOfflineRenderFormat).
    /// </summary>
    public const int kAudioQueueErr_InvalidOfflineMode   = -66626;
}