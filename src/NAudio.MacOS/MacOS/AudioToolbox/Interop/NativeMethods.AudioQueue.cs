// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox/audio-queue-services for more information.

using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.MacOS.AudioToolbox.Interop;

// The AudioQueueNewOutputWithDispatchQueue and AudioQueueNewInputWithDispatchQueue functions are
// intentionally not ported: they take Objective-C blocks, which cannot be expressed in C#.
internal static partial class NativeMethods
{
    #region Creating and destroying queues

    /*
    extern OSStatus
    AudioQueueNewOutput(                const AudioStreamBasicDescription *inFormat,
                                        AudioQueueOutputCallback        inCallbackProc,
                                        void * __nullable               inUserData,
                                        CFRunLoopRef __nullable         inCallbackRunLoop,
                                        CFStringRef __nullable          inCallbackRunLoopMode,
                                        UInt32                          inFlags,
                                        AudioQueueRef __nullable * __nonnull outAQ)          API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Creates a new audio queue for playing audio data.</summary>
    /// <param name="inFormat">A pointer to a structure describing the format of the audio data to be played. For linear PCM, only interleaved formats are supported. Compressed formats are supported.</param>
    /// <param name="inCallbackProc">A pointer to a callback function to be called when the audio queue has finished playing a buffer.</param>
    /// <param name="inUserData">A value or pointer to data that you specify to be passed to the callback function.</param>
    /// <param name="inCallbackRunLoop">The event loop on which inCallbackProc is to be called. If you specify NULL, the callback is called on one of the audio queue's internal threads.</param>
    /// <param name="inCallbackRunLoopMode">The run loop mode in which to call the callback. Typically, you pass kCFRunLoopCommonModes. (NULL also specifies kCFRunLoopCommonModes). Other possibilities are implementation specific. You can choose to create your own thread with your own run loops. For more information on run loops, see Run Loops or CFRunLoop Reference.</param>
    /// <param name="inFlags">Reserved for future use. Pass 0.</param>
    /// <param name="outAQ">On return, this variable contains a pointer to the newly created playback audio queue object.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// To create an playback audio queue, you allocate buffers, then queue buffers (using
    /// AudioQueueEnqueueBuffer). The callback receives buffers and typically queues them again.
    /// To schedule a buffer for playback, providing parameter and start time information, call
    /// AudioQueueEnqueueBufferWithParameters.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueNewOutput(
        in AudioStreamBasicDescription inFormat,
        AudioQueueOutputCallback inCallbackProc,
        IntPtr inUserData,
        IntPtr inCallbackRunLoop,
        IntPtr inCallbackRunLoopMode,
        uint inFlags,
        out IntPtr outAQ
    );

    /*
    extern OSStatus
    AudioQueueNewInput(                 const AudioStreamBasicDescription *inFormat,
                                        AudioQueueInputCallback         inCallbackProc,
                                        void * __nullable               inUserData,
                                        CFRunLoopRef __nullable         inCallbackRunLoop,
                                        CFStringRef __nullable          inCallbackRunLoopMode,
                                        UInt32                          inFlags,
                                        AudioQueueRef __nullable * __nonnull outAQ)          API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Creates a new audio queue for recording audio data.</summary>
    /// <param name="inFormat">A pointer to a structure describing the format of the audio data to be recorded. For linear PCM, only interleaved formats are supported. Compressed formats are supported.</param>
    /// <param name="inCallbackProc">A pointer to a callback function to be called when the audio queue has finished filling a buffer.</param>
    /// <param name="inUserData">A value or pointer to data that you specify to be passed to the callback function.</param>
    /// <param name="inCallbackRunLoop">The event loop on which inCallbackProc is to be called. If you specify NULL, the callback is called on one of the audio queue's internal threads.</param>
    /// <param name="inCallbackRunLoopMode">The run loop mode in which to call the callback. Typically, you pass kCFRunLoopCommonModes. (NULL also specifies kCFRunLoopCommonModes). Other possibilities are implementation specific. You can choose to create your own thread with your own run loops. For more information on run loops, see Run Loops or CFRunLoop Reference.</param>
    /// <param name="inFlags">Reserved for future use. Pass 0.</param>
    /// <param name="outAQ">On return, this variable contains a pointer to the newly created recording audio queue object.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Outline of how to use the queue for input: <br /> <br />
    /// - create input queue
    /// - allocate buffers
    /// - enqueue buffers (AudioQueueEnqueueBuffer, not with parameters, no packet descriptions)
    /// - the callback receives buffers and re-enqueues them
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueNewInput(
        in AudioStreamBasicDescription inFormat,
        AudioQueueInputCallback inCallbackProc,
        IntPtr inUserData,
        IntPtr inCallbackRunLoop,
        IntPtr inCallbackRunLoopMode,
        uint inFlags,
        out IntPtr outAQ
    );

    /*
    extern OSStatus
    AudioQueueDispose(                  AudioQueueRef           inAQ,
                                        Boolean                 inImmediate)            API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Disposes an existing audio queue.</summary>
    /// <param name="inAQ">The audio queue you want to dispose of</param>
    /// <param name="inImmediate">If you pass true, the audio queue is disposed of immediately (that is, synchronously). If you pass false, disposal does not take place until all enqueued buffers are processed. Whether you call AudioQueueDispose synchronously or asynchronously, you can no longer interact with the queue, and the queue no longer invokes any callbacks to your application after the function returns. Note that if AudioQueueDispose is called from a buffer completion callback or property listener, you may receive further callbacks afterwards.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Disposing of the audio queue also disposes of all its resources, including its buffers.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueDispose(IntPtr inAQ, MacBoolean inImmediate);

    #endregion

    #region Buffer management

    /*
    extern OSStatus
    AudioQueueAllocateBuffer(           AudioQueueRef           inAQ,
                                        UInt32                  inBufferByteSize,
                                        AudioQueueBufferRef __nullable * __nonnull outBuffer)              API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Asks an audio queue to allocate a buffer.</summary>
    /// <param name="inAQ">The audio queue you want to allocate a buffer.</param>
    /// <param name="inBufferByteSize">The desired size of the new buffer, in bytes. An appropriate buffer size depends on the processing you will perform on the data as well as on the audio data format.</param>
    /// <param name="outBuffer">On return, points to the newly created audio buffer. The mAudioDataByteSize field in the audio queue buffer structure, AudioQueueBuffer, is initially set to 0.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Once allocated, the pointer to the buffer and the buffer's size are fixed and cannot be
    /// changed. The mAudioDataByteSize field in the audio queue buffer structure,
    /// AudioQueueBuffer, is initially set to 0.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueAllocateBuffer(IntPtr inAQ, uint inBufferByteSize, out AudioQueueBuffer* outBuffer);

    /*
    extern OSStatus
    AudioQueueAllocateBufferWithPacketDescriptions(
                                        AudioQueueRef           inAQ,
                                        UInt32                  inBufferByteSize,
                                        UInt32                  inNumberPacketDescriptions,
                                        AudioQueueBufferRef __nullable * __nonnull outBuffer)              API_AVAILABLE(macos(10.6), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Asks an audio queue to allocate a buffer with space for packet descriptions.</summary>
    /// <param name="inAQ">The audio queue you want to allocate a buffer.</param>
    /// <param name="inBufferByteSize">The desired size of the new buffer, in bytes. An appropriate buffer size depends on the processing you will perform on the data as well as on the audio data format.</param>
    /// <param name="inNumberPacketDescriptions">The desired capacity of the packet description array in the new buffer.</param>
    /// <param name="outBuffer">On return, points to the newly created audio buffer. The mAudioDataByteSize field in the audio queue buffer structure, AudioQueueBuffer, is initially set to 0.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Once allocated, the pointer to the buffer and the buffer's size are fixed and cannot be
    /// changed. The mAudioDataByteSize field in the audio queue buffer structure,
    /// AudioQueueBuffer, is initially set to 0.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.6")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueAllocateBufferWithPacketDescriptions(
        IntPtr inAQ,
        uint inBufferByteSize,
        uint inNumberPacketDescriptions,
        out AudioQueueBuffer* outBuffer
    );

    /*
    extern OSStatus
    AudioQueueFreeBuffer(               AudioQueueRef           inAQ,
                                        AudioQueueBufferRef     inBuffer)           API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Disposes of an audio queue buffer.</summary>
    /// <param name="inAQ">The queue from which the buffer was allocated.</param>
    /// <param name="inBuffer">The buffer to be disposed.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function disposes of the buffer allocated by AudioQueueAllocateBuffer. Disposing of
    /// an audio queue also automatically disposes of any associated buffers and timeline
    /// objects. Call this function only if you want to dispose of a particular buffer while
    /// continuing to use an audio queue. You can dispose of buffers only when the associated
    /// queue is stopped (that is, not processing audio data).
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueFreeBuffer(IntPtr inAQ, AudioQueueBuffer* inBuffer);

    /*
    extern OSStatus
    AudioQueueEnqueueBuffer(            AudioQueueRef                       inAQ,
                                        AudioQueueBufferRef                 inBuffer,
                                        UInt32                              inNumPacketDescs,
                                        const AudioStreamPacketDescription * __nullable inPacketDescs)      API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Assigns a buffer to an audio queue for recording or playback.</summary>
    /// <param name="inAQ">The audio queue you are assigning the buffer to.</param>
    /// <param name="inBuffer">The buffer to queue (that is, to be recorded into or played from).</param>
    /// <param name="inNumPacketDescs">The number of packet descriptions pointed to by the inPacketDescs pointer. Applicable only for output queues and required only for variable-bit-rate (VBR) audio formats. Pass 0 for input queues (no packet descriptions are required).</param>
    /// <param name="inPacketDescs">An array of packet descriptions. Applicable only for output queues and required only for variable-bit-rate (VBR) audio formats. Pass NULL for input queues (no packet descriptions are required).</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// If the buffer was allocated with AudioQueueAllocateBufferWithPacketDescriptions,
    /// the client should provide packet descriptions in the buffer's mPacketDescriptions
    /// and mPacketDescriptionCount fields rather than in inPacketDescs and
    /// inNumPacketDescs, which should be NULL and 0, respectively, in this case. <br /> <br />
    /// For an input queue, pass 0 and NULL for inNumPacketDescs and inPacketDescs,
    /// respectively. Your callback will receive packet descriptions owned by the audio queue.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueEnqueueBuffer(
        IntPtr inAQ,
        AudioQueueBuffer* inBuffer,
        uint inNumPacketDescs,
        AudioStreamPacketDescription* inPacketDescs
    );

    /*
    extern OSStatus
    AudioQueueEnqueueBufferWithParameters(
                                        AudioQueueRef                                inAQ,
                                        AudioQueueBufferRef                          inBuffer,
                                        UInt32                                       inNumPacketDescs,
                                        const AudioStreamPacketDescription * __nullable inPacketDescs,
                                        UInt32                                       inTrimFramesAtStart,
                                        UInt32                                       inTrimFramesAtEnd,
                                        UInt32                                       inNumParamValues,
                                        const AudioQueueParameterEvent * __nullable  inParamValues,
                                        const AudioTimeStamp * __nullable            inStartTime,
                                        AudioTimeStamp * __nullable                  outActualStartTime)     API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Assigns a buffer to an audio queue for playback, providing parameters and start time information.</summary>
    /// <param name="inAQ">The audio queue associated with the buffer.</param>
    /// <param name="inBuffer">The buffer to be played from.</param>
    /// <param name="inNumPacketDescs">The number of packet descriptions pointed to by the inPacketDescs parameter. Required only for variable-bit-rate (VBR) audio formats. Pass 0 if no packet descriptions are required.</param>
    /// <param name="inPacketDescs">A pointer to an array of audio stream packet descriptions. Required only for VBR audio formats. Pass NULL if no packet descriptions are required.</param>
    /// <param name="inTrimFramesAtStart">The number of priming frames to skip at the start of the buffer.</param>
    /// <param name="inTrimFramesAtEnd">The number of frames to skip at the end of the buffer.</param>
    /// <param name="inNumParamValues">The number of parameter values pointed to by the inParamValues parameter.</param>
    /// <param name="inParamValues">An array of parameter values. (In macOS v10.5, there is only one parameter, kAudioQueueParam_Volume.) These values are set before buffer playback and cannot be changed while the buffer is playing. How accurately changes in parameters can be scheduled depends on the size of the buffer. If there are no parameters to set (inNumParamValues = 0), pass NULL.</param>
    /// <param name="inStartTime">A pointer to a structure containing the desired start time for playing the buffer. If you specify the time using the mSampleTime field of the AudioTimeStamp structure, the sample time is relative to the time the queue started. If you pass NULL for the start time, the buffer starts immediately after the previously queued buffer, or as soon as possible if no buffers are queued ahead of it. Buffers are played in the order they are queued. If multiple buffers are queued, their times must be in ascending order or NULL; otherwise, an error occurs. The start time indicates when the actual audio data in the buffer is to be played (that is, the trim frames are not counted). Note: When specifying a start time for a buffer, if the buffer is not the first enqueued since AudioQueueStop or AudioQueueReset, it is normally necessary to call AudioQueueFlush before AudioQueueEnqueueBufferWithParameters.</param>
    /// <param name="outActualStartTime">On return, points to an AudioTimeStamp structure indicating when the buffer will actually play.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// You can exert some control of the buffer queue by using this function. You can assign
    /// audio queue settings that are in effect carried by an audio queue buffer as you enqueue
    /// it. Hence, these changes only take effect when an audio queue buffer begins playing. <br /> <br />
    /// This function queues a buffer for playback only, not for recording. Audio queues for
    /// recording have no parameters, do not support variable-bit-rate (VBR) formats (which
    /// might require trimming), and have a different way to handle timing. When queued for
    /// playback, the buffer must contain the audio data to be played back. See
    /// AudioQueueEnqueueBuffer for details on queuing a buffer for recording. <br /> <br />
    /// If the buffer was allocated with AudioQueueAllocateBufferWithPacketDescriptions,
    /// the client should provide packet descriptions in the buffer's mPacketDescriptions
    /// and mPacketDescriptionCount fields rather than in inPacketDescs and
    /// inNumPacketDescs, which should be NULL and 0, respectively, in this case.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueEnqueueBufferWithParameters(
        IntPtr inAQ,
        AudioQueueBuffer* inBuffer,
        uint inNumPacketDescs,
        AudioStreamPacketDescription* inPacketDescs,
        uint inTrimFramesAtStart,
        uint inTrimFramesAtEnd,
        uint inNumParamValues,
        AudioQueueParameterEvent* inParamValues,
        AudioTimeStamp* inStartTime,
        AudioTimeStamp* outActualStartTime
    );

    #endregion

    #region Queue control

    /*
    extern OSStatus
    AudioQueueStart(                    AudioQueueRef                     inAQ,
                                        const AudioTimeStamp * __nullable inStartTime)        API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Begins playing or recording audio.</summary>
    /// <param name="inAQ">The audio queue to start.</param>
    /// <param name="inStartTime">A pointer to the time at which the audio queue should start. If you specify the time using the mSampleTime field of the AudioTimeStamp structure, the sample time is referenced to the sample frame timeline of the associated audio device. May be NULL.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// If the audio hardware is not already running, this function starts it.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueStart(IntPtr inAQ, AudioTimeStamp* inStartTime);

    /*
    extern OSStatus
    AudioQueuePrime(                    AudioQueueRef           inAQ,
                                        UInt32                  inNumberOfFramesToPrepare,
                                        UInt32 * __nullable     outNumberOfFramesPrepared)  API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Begins decoding buffers in preparation for playback.</summary>
    /// <param name="inAQ">The audio queue to be primed.</param>
    /// <param name="inNumberOfFramesToPrepare">The number of frames to decode before returning. Pass 0 to decode all enqueued buffers.</param>
    /// <param name="outNumberOfFramesPrepared">If not NULL, on return, a pointer to the number of frames actually decoded and prepared for playback.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function begins decoding buffers in preparation for playback. It returns when at
    /// least the number of audio sample frames are decoded and ready to play or when all
    /// enqueued buffers have been completely decoded. To ensure that a buffer has been decoded
    /// and is completely ready for playback, before playback: <br /> <br />
    /// 1. Call AudioQueueEnqueueBuffer.
    /// 2. Call AudioQueuePrime, which waits if you pass 0 to have a default number of
    /// frames decoded.
    /// 3. Call AudioQueueStart. <br /> <br />
    /// Calls to AudioQueuePrime following AudioQueueStart/AudioQueuePrime, and before
    /// AudioQueueReset/AudioQueueStop, will have no useful effect. In this situation,
    /// outNumberOfFramesPrepared will not have a useful return value.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueuePrime(IntPtr inAQ, uint inNumberOfFramesToPrepare, uint* outNumberOfFramesPrepared);

    /*
    extern OSStatus
    AudioQueueStop(                     AudioQueueRef           inAQ,
                                        Boolean                 inImmediate)            API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Stops playing or recording audio.</summary>
    /// <param name="inAQ">The audio queue to stop.</param>
    /// <param name="inImmediate">If you pass true, the stop request occurs immediately (that is, synchronously), and the function returns when the audio queue has stopped. Buffer callbacks are invoked during the stopping. If you pass false, the function returns immediately, but the queue does not stop until all its queued buffers are played or filled (that is, the stop occurs asynchronously). Buffer callbacks are invoked as necessary until the queue actually stops. Also, a playback audio queue callback calls this function when there is no more audio to play. Note that when stopping immediately, all pending buffer callbacks are normally invoked during the process of stopping. But if the calling thread is responding to a buffer callback, then it is possible for additional buffer callbacks to occur after AudioQueueStop returns.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function resets the audio queue and stops the audio hardware associated with the
    /// queue if it is not in use by other audio services. Synchronous stops occur immediately,
    /// regardless of previously buffered audio data. Asynchronous stops occur after all queued
    /// buffers have been played or recorded.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueStop(IntPtr inAQ, MacBoolean inImmediate);

    /*
    extern OSStatus
    AudioQueuePause(                    AudioQueueRef           inAQ)       API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Pauses audio playback or recording.</summary>
    /// <param name="inAQ">The queue to be paused.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Pausing the queue does not affect buffers or reset the audio queue. To resume playback
    /// or recording using the audio queue, call AudioQueueStart.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueuePause(IntPtr inAQ);

    /*
    extern OSStatus
    AudioQueueFlush(                    AudioQueueRef           inAQ)            API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Resets the audio queue's decoder state.</summary>
    /// <param name="inAQ">The audio queue to be flushed.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// After all queued buffers have been played, the function cleans up all decoder state
    /// information. You must call this function following a sequence of buffers of encoded
    /// audio; otherwise, some of the audio might not play in the next set of queued buffers.
    /// The only time it is not necessary to call AudioQueueFlush is following AudioQueueStop
    /// with inImmediate=false. (This action internally calls AudioQueueFlush.) <br /> <br />
    /// Also, you might wish to call this function before calling AudioQueueStop depending on
    /// whether you want to stop immediately regardless of what has played or whether you want
    /// to ensure that all buffered data and all data that is in the middle of processing gets
    /// recorded or played before stopping.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueFlush(IntPtr inAQ);

    /*
    extern OSStatus
    AudioQueueReset(                    AudioQueueRef           inAQ)            API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Resets an audio queue.</summary>
    /// <param name="inAQ">The audio queue to reset.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function immediately resets an audio queue, flushes any queued buffer, removes all
    /// buffers from previously scheduled use, and resets any decoder and digital signal
    /// processing (DSP) state information. It also invokes callbacks for any flushed buffers.
    /// If you queue any buffers after calling this function, processing does not occur until
    /// the decoder and DSP state information is reset. Hence, a discontinuity (that is, a
    /// "glitch") might occur. <br /> <br />
    /// Note that when resetting, all pending buffer callbacks are normally invoked
    /// during the process of resetting. But if the calling thread is responding to a buffer
    /// callback, then it is possible for additional buffer callbacks to occur after
    /// AudioQueueReset returns.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueReset(IntPtr inAQ);

    #endregion

    #region Parameter management

    /*
    extern OSStatus
    AudioQueueGetParameter(             AudioQueueRef               inAQ,
                                        AudioQueueParameterID       inParamID,
                                        AudioQueueParameterValue *  outValue)       API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtains an audio queue parameter value.</summary>
    /// <param name="inAQ">The audio queue whose parameter value you want to obtain.</param>
    /// <param name="inParamID">The ID of the parameter you want to obtain. In macOS v10.5, audio queues have one parameter available: kAudioQueueParam_Volume, which controls the queue's playback volume.</param>
    /// <param name="outValue">On return, points to the current value of the specified parameter.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// You can access the current parameter values for an audio queue at any time with this
    /// function.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueGetParameter(IntPtr inAQ, AudioQueueParameterID inParamID, out float outValue);

    /*
    extern OSStatus
    AudioQueueSetParameter(             AudioQueueRef               inAQ,
                                        AudioQueueParameterID       inParamID,
                                        AudioQueueParameterValue    inValue)        API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Sets an audio queue parameter value.</summary>
    /// <param name="inAQ">The audio queue whose parameter value you want to set.</param>
    /// <param name="inParamID">The ID of the parameter you want to set.</param>
    /// <param name="inValue">The parameter value to set.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueSetParameter(IntPtr inAQ, AudioQueueParameterID inParamID, float inValue);

    #endregion

    #region Property management

    /*
    extern OSStatus
    AudioQueueGetProperty(              AudioQueueRef           inAQ,
                                        AudioQueuePropertyID    inID,
                                        void *                  outData,
                                        UInt32 *                ioDataSize)             API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtains an audio queue property value.</summary>
    /// <param name="inAQ">The audio queue whose property value you want to obtain.</param>
    /// <param name="inID">The ID of the property you want to obtain. See "Audio Queue Property IDs."</param>
    /// <param name="outData">On return, points to the desired property value.</param>
    /// <param name="ioDataSize">A pointer to the size of the property data. On input, points to the maximum bytes of space the caller expects to receive. On return, points to the actual data size.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueGetProperty(
        IntPtr inAQ,
        AudioQueuePropertyID inID,
        IntPtr outData,
        ref uint ioDataSize
    );

    /*
    extern OSStatus
    AudioQueueSetProperty(              AudioQueueRef           inAQ,
                                        AudioQueuePropertyID    inID,
                                        const void *            inData,
                                        UInt32                  inDataSize)             API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Sets an audio queue property value.</summary>
    /// <param name="inAQ">The audio queue whose property value you want to set.</param>
    /// <param name="inID">The ID of the property you want to set. See "Audio Queue Property IDs" for the various audio queue properties.</param>
    /// <param name="inData">A pointer to the property value to set.</param>
    /// <param name="inDataSize">The size of the property data.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueSetProperty(
        IntPtr inAQ,
        AudioQueuePropertyID inID,
        IntPtr inData,
        uint inDataSize
    );

    /*
    extern OSStatus
    AudioQueueGetPropertySize(          AudioQueueRef           inAQ,
                                        AudioQueuePropertyID    inID,
                                        UInt32 *                outDataSize)            API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtains the size of an audio queue property.</summary>
    /// <param name="inAQ">The audio queue containing the property value whose size you want to obtain.</param>
    /// <param name="inID">The ID of the property value whose size you want to obtain. See "Audio Queue Property IDs" for possible values.</param>
    /// <param name="outDataSize">On return, points to the size of the specified property value.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueGetPropertySize(IntPtr inAQ, AudioQueuePropertyID inID, out uint outDataSize);

    /*
    extern OSStatus
    AudioQueueAddPropertyListener(      AudioQueueRef                   inAQ,
                                        AudioQueuePropertyID            inID,
                                        AudioQueuePropertyListenerProc  inProc,
                                        void * __nullable               inUserData)     API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Adds a listener callback for a property.</summary>
    /// <param name="inAQ">The audio queue that owns the property you want to assign the listener callback to.</param>
    /// <param name="inID">The ID of the property to which you want to assign a listener callback. See "Audio Queue Property IDs".</param>
    /// <param name="inProc">The listener callback to be called when the property value changes.</param>
    /// <param name="inUserData">A value to be passed to the listener callback when it is called.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This callback is used to act upon a change in an audio queue property such as
    /// kAudioQueueProperty_IsRunning. For instance, if your application has a user interface
    /// with a Play/Stop button, and kAudioQueueProperty_IsRunning changes, you need to update
    /// your button.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueAddPropertyListener(
        IntPtr inAQ,
        AudioQueuePropertyID inID,
        AudioQueuePropertyListenerProc inProc,
        IntPtr inUserData
    );

    /*
    extern OSStatus
    AudioQueueRemovePropertyListener(   AudioQueueRef                   inAQ,
                                        AudioQueuePropertyID            inID,
                                        AudioQueuePropertyListenerProc  inProc,
                                        void * __nullable               inUserData)     API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Removes a listener callback for a property.</summary>
    /// <param name="inAQ">The audio queue that owns the property from which you want to remove a listener.</param>
    /// <param name="inID">The ID of the property from which you want to remove a listener.</param>
    /// <param name="inProc">The listener being removed.</param>
    /// <param name="inUserData">The same inUserData value that was previously passed to AudioQueueAddPropertyListener.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueRemovePropertyListener(
        IntPtr inAQ,
        AudioQueuePropertyID inID,
        AudioQueuePropertyListenerProc inProc,
        IntPtr inUserData
    );

    #endregion

    #region Handling timing

    /*
    extern OSStatus
    AudioQueueCreateTimeline(           AudioQueueRef           inAQ,
                                        AudioQueueTimelineRef __nullable * __nonnull outTimeline)            API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Creates a timeline object.</summary>
    /// <param name="inAQ">The audio queue to associate with the new timeline object.</param>
    /// <param name="outTimeline">On return, points to the newly created timeline object.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// You need to instantiate a timeline object if you want to know about any timeline
    /// discontinuities. See AudioQueueGetCurrentTime for more details.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueCreateTimeline(IntPtr inAQ, out IntPtr outTimeline);

    /*
    extern OSStatus
    AudioQueueDisposeTimeline(          AudioQueueRef           inAQ,
                                        AudioQueueTimelineRef   inTimeline)             API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Disposes of a timeline object.</summary>
    /// <param name="inAQ">The audio queue associated with the timeline object you want to dispose of.</param>
    /// <param name="inTimeline">The timeline object to dispose of.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// Disposing of an audio queue automatically disposes of any associated timeline objects.
    /// Call this function only if you want to dispose of a timeline object and not the audio
    /// queue associated with it.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueDisposeTimeline(IntPtr inAQ, IntPtr inTimeline);

    /*
    extern OSStatus
    AudioQueueGetCurrentTime(           AudioQueueRef                    inAQ,
                                        AudioQueueTimelineRef __nullable inTimeline,
                                        AudioTimeStamp * __nullable      outTimeStamp,
                                        Boolean * __nullable             outTimelineDiscontinuity)       API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtains the current audio queue time.</summary>
    /// <param name="inAQ">The audio queue whose current time you want to obtain.</param>
    /// <param name="inTimeline">The audio queue timeline object to which any timeline discontinuities are reported. May be NULL.</param>
    /// <param name="outTimeStamp">On return, points to an audio timestamp structure containing the current audio queue time. The mSampleTime field is in terms of the audio queue's sample rate, and relative to the time at which the queue has started or will start.</param>
    /// <param name="outTimelineDiscontinuity">Can be NULL. On return, only set to true or false if the inTimeLine parameter is not NULL. Set to true if a discontinuity has occurred in the sample timeline of the audio queue. For instance, the device's sample rate changed and a gap occurred in playback or recording, or the audio queue was unable to prepare and playback in time because it was late.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// You must specify a timeline object if you want to be notified about any timeline
    /// discontinuities in the outTimelineDiscontinuity parameter. If you don't care about
    /// discontinuities, pass NULL in the inTimeLine and outTimelineDiscontinuity parameters.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueGetCurrentTime(
        IntPtr inAQ,
        IntPtr inTimeline,
        AudioTimeStamp* outTimeStamp,
        MacBoolean* outTimelineDiscontinuity
    );

    /*
    extern OSStatus
    AudioQueueDeviceGetCurrentTime(     AudioQueueRef           inAQ,
                                        AudioTimeStamp *        outTimeStamp)      API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtains the current time of the audio device associated with an audio queue.</summary>
    /// <param name="inAQ">The audio queue whose audio device is to be queried.</param>
    /// <param name="outTimeStamp">A pointer to a structure that, on return, contains the current time of the audio device associated with the audio queue.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// If the audio device associated with the audio queue is not running, the only valid field
    /// in the audio timestamp structure is mHostTime. This result differentiates the action of
    /// this function from that of the AudioDeviceGetCurrentTime function, (declared in
    /// AudioHardware.h) which returns an error if the audio device is not running.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueDeviceGetCurrentTime(IntPtr inAQ, out AudioTimeStamp outTimeStamp);

    /*
    extern OSStatus
    AudioQueueDeviceTranslateTime(      AudioQueueRef           inAQ,
                                        const AudioTimeStamp *  inTime,
                                        AudioTimeStamp *        outTime)        API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Converts the time in the time base of the associated audio device from one representation to another.</summary>
    /// <param name="inAQ">The queue whose audio device is to perform the requested time translation.</param>
    /// <param name="inTime">A pointer to a structure containing the time to be translated.</param>
    /// <param name="outTime">On entry, mFlags indicate the desired translations. On exit, mFlags indicates which of the requested translated fields were successfully populated.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function converts from one time representation to another (for example, from sample
    /// time to host time or vice versa): <br /> <br />
    /// - Sample time is the absolute sample frame time. Sample numbers are the count of the samples
    /// on the audio device.
    /// - Host time is the time base of the host machine such as the time of the bus clock on the CPU. <br /> <br />
    /// The mSampleTime field in the AudioTimestamp structure (described in Core Audio Data
    /// Types Reference) is always in device time, not in audio queue time. Audio queue time is
    /// relative to the audio queue's start time. The associated audio device has to be running
    /// for the AudioQueueDeviceTranslateTime function to provide a result.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueDeviceTranslateTime(IntPtr inAQ, in AudioTimeStamp inTime, out AudioTimeStamp outTime);

    /*
    extern OSStatus
    AudioQueueDeviceGetNearestStartTime(AudioQueueRef           inAQ,
                                        AudioTimeStamp *        ioRequestedStartTime,
                                        UInt32                  inFlags)        API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtains an audio device's start time that is closest to a requested start time.</summary>
    /// <param name="inAQ">The audio queue whose device's nearest start time you want to obtain.</param>
    /// <param name="ioRequestedStartTime">On entry, points to the requested start time. On return, points to the actual start time.</param>
    /// <param name="inFlags">Reserved for future use. Pass 0.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueDeviceGetNearestStartTime(IntPtr inAQ, ref AudioTimeStamp ioRequestedStartTime, uint inFlags);

    #endregion

    #region Offline rendering

    /*
    extern OSStatus
    AudioQueueSetOfflineRenderFormat(   AudioQueueRef                                  inAQ,
                                        const AudioStreamBasicDescription * __nullable inFormat,
                                        const AudioChannelLayout * __nullable          inLayout)
    																		API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Specify an audio format to which the queue will perform subsequent offline rendering, or disable offline rendering.</summary>
    /// <param name="inAQ">The output queue whose offline rendering mode is to be changed.</param>
    /// <param name="inFormat">The desired format for offline rendering. Pass NULL to disable offline rendering and return the queue to normal output to an audio device. This format must be linear PCM and (if not mono) interleaved.</param>
    /// <param name="inLayout">The desired channel layout for offline rendering; also NULL when disabling offline rendering.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// An output queue's audio playback can be redirected for capture to an audio file,
    /// to support an export function, for example. AudioQueueSetOfflineRenderFormat switches
    /// a queue between normal and offline rendering modes.
    /// </remarks>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueSetOfflineRenderFormat(IntPtr inAQ, AudioStreamBasicDescription* inFormat, AudioChannelLayout* inLayout);

    /*
    extern OSStatus
    AudioQueueOfflineRender(            AudioQueueRef           inAQ,
                                        const AudioTimeStamp *  inTimestamp,
                                        AudioQueueBufferRef     ioBuffer,
                                        UInt32                  inNumberFrames)     API_AVAILABLE(macos(10.5), ios(2.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Obtain a buffer of audio output from a queue in offline rendering mode.</summary>
    /// <param name="inAQ">The output queue from which to obtain output.</param>
    /// <param name="inTimestamp">The point in time corresponding to the beginning of the output buffer. Only mSampleTime is used. mFlags must include kAudioTimeStampSampleTimeValid.</param>
    /// <param name="ioBuffer">The buffer into which the queue will render.</param>
    /// <param name="inNumberFrames">The number of frames of audio to render. Note that fewer frames than requested may be returned. This can happen if insufficient data was enqueued.</param>
    /// <returns>An OSStatus result code.</returns>
    [SupportedOSPlatform("ios2.0")]
    [SupportedOSPlatform("macos10.5")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueOfflineRender(
        IntPtr inAQ,
        in AudioTimeStamp inTimestamp,
        AudioQueueBuffer* ioBuffer,
        uint inNumberFrames
    );

    #endregion

    #region Processing taps

    /*
    extern OSStatus
    AudioQueueProcessingTapNew(         AudioQueueRef                   inAQ,
                                        AudioQueueProcessingTapCallback inCallback,
                                        void * __nullable               inClientData,
                                        AudioQueueProcessingTapFlags    inFlags,
                                        UInt32 *                        outMaxFrames,
                                        AudioStreamBasicDescription *   outProcessingFormat,
                                        AudioQueueProcessingTapRef __nullable * __nonnull outAQTap)
                                            API_AVAILABLE(macos(10.7), ios(6.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Create a new processing tap</summary>
    /// <param name="inAQ">The audio queue from which to create the processing tap</param>
    /// <param name="inCallback">A callback which the queue will call to process the audio</param>
    /// <param name="inClientData">Client data provided to the callback</param>
    /// <param name="inFlags">Flags that are used to control aspects of the processing tap. Valid flags are: - kAudioQueueProcessingTap_PreEffects: processing is done before any further effects are applied by the audio queue to the audio - kAudioQueueProcessingTap_PostEffects: processing is done after all processing is done, including that of other taps. - kAudioQueueProcessingTap_Siphon</param>
    /// <param name="outMaxFrames">The maximum number of sample frames that can be requested of a processing tap at any one time. Typically this will be approximately 50 msec of audio (2048 samples @ 44.1kHz)</param>
    /// <param name="outProcessingFormat">The format in which the client will receive the audio data to be processed. This will always be the same sample rate as the client format and usually the same number of channels as the client format of the audio queue. (NOTE: the number of channels may be different in some cases if the client format has some channel count restrictions, for instance the client provides 5.1 AAC, but the decoder can only produce stereo). The channel order, if the same as the client format, will be the same as the client channel order. If the channel count is changed, it will be to either 1 (mono) or 2 (stereo, in which case the first channel is left, the second right). If the data is not in a convenient format for the client to process in, then the client should convert the data to and from that format. This is the most efficient mechanism to use (as the audio queue can chose a format that is most efficient from its playback (or recording) requirement.</param>
    /// <param name="outAQTap">The processing tap object.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function creates a processing tap on a given audio queue. A
    /// processing tap can only be established (or removed) on an audio queue that is
    /// stopped (paused is not sufficient). The processing tap will then be used to
    /// process either decoded data in the case of an output queue, or input data
    /// (before it is encoded) in the case of an input queue. <br /> <br />
    /// The processing is performed on audio either before or after any effects or other
    /// processing (varispeed, etc) is applied by the audio queue, depending on inFlags.
    /// </remarks>
    [SupportedOSPlatform("ios6.0")]
    [SupportedOSPlatform("macos10.7")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static unsafe partial int AudioQueueProcessingTapNew(
        IntPtr inAQ,
        AudioQueueProcessingTapCallback inCallback,
        IntPtr inClientData,
        AudioQueueProcessingTapFlags inFlags,
        out uint outMaxFrames,
        out AudioStreamBasicDescription outProcessingFormat,
        out IntPtr outAQTap
    );

    /*
    extern OSStatus
    AudioQueueProcessingTapDispose(     AudioQueueProcessingTapRef      inAQTap)
                                            API_AVAILABLE(macos(10.7), ios(6.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Dispose a processing tap object</summary>
    /// <param name="inAQTap">The processing tap to dispose.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// As with AudioQueueProcessingTapNew(), this call can only be made on an
    /// audio queue that is stopped (paused is not sufficient)
    /// </remarks>
    [SupportedOSPlatform("ios6.0")]
    [SupportedOSPlatform("macos10.7")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueProcessingTapDispose(IntPtr inAQTap);

    /*
    extern OSStatus
    AudioQueueProcessingTapGetSourceAudio(
                                        AudioQueueProcessingTapRef      inAQTap,
                                        UInt32                          inNumberFrames,
                                        AudioTimeStamp *                ioTimeStamp,
                                        AudioQueueProcessingTapFlags *  outFlags,
                                        UInt32 *                        outNumberFrames,
                                        AudioBufferList *               ioData)
                                            API_AVAILABLE(macos(10.7), ios(6.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Used by a processing tap to retrieve source audio.</summary>
    /// <param name="inAQTap">the processing tap</param>
    /// <param name="inNumberFrames">the number of frames the processing tap requires for its processing</param>
    /// <param name="ioTimeStamp">On an input audio queue, the timestamp is returned from this function. On an output audio queue, the caller must provide a continuous timestamp.</param>
    /// <param name="outFlags">flags to describe state about the input requested, e.g. discontinuity/complete</param>
    /// <param name="outNumberFrames">the number of source frames that have been provided by the parent audio queue. This can be less than the number of requested frames specified in inNumberFrames</param>
    /// <param name="ioData">the audio buffer list which will contain the source data. The audio queue owns the buffer pointers if NULL pointers were provided (recommended). In this case the source buffers are only valid for the duration of the processing tap callback. If the buffer pointers are non-NULL, then they must be big enough to hold inNumberFrames, and the audio queue will copy its source data into those buffers.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function may only be called from the processing tap's callback.
    /// </remarks>
    [SupportedOSPlatform("ios6.0")]
    [SupportedOSPlatform("macos10.7")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueProcessingTapGetSourceAudio(
        IntPtr inAQTap,
        uint inNumberFrames,
        ref AudioTimeStamp ioTimeStamp,
        out AudioQueueProcessingTapFlags outFlags,
        out uint outNumberFrames,
        IntPtr ioData
    );

    /*
    extern OSStatus
    AudioQueueProcessingTapGetQueueTime(    AudioQueueProcessingTapRef  inAQTap,
                                            Float64 *                   outQueueSampleTime,
                                            UInt32 *                    outQueueFrameCount)
                                            API_AVAILABLE(macos(10.8), ios(6.0), watchos(2.0), tvos(9.0));
    */

    /// <summary>Used by a processing tap to retrieve the queue's current time.</summary>
    /// <param name="inAQTap">the processing tap</param>
    /// <param name="outQueueSampleTime">the current sample time of the audio queue. This will appear to be stationary if the queue is paused.</param>
    /// <param name="outQueueFrameCount">the number of sample frames of queue time corresponding to the current chunk of audio being processed by the tap. This will differ from the frame count passed to the tap if the queue's playback rate is currently other than 1.0, due to the use of time compression/expansion. The frame count can also be 0 if the queue is paused.</param>
    /// <returns>An OSStatus result code.</returns>
    /// <remarks>
    /// This function may only be called from the processing tap's callback, and only
    /// for audio output queues. It must be called after calling
    /// AudioQueueProcessingTapGetSourceAudio().
    /// </remarks>
    [SupportedOSPlatform("ios6.0")]
    [SupportedOSPlatform("macos10.8")]
    [LibraryImport(MacLibraries.AudioToolbox)]
    public static partial int AudioQueueProcessingTapGetQueueTime(IntPtr inAQTap, out double outQueueSampleTime, out uint outQueueFrameCount);

    #endregion
}
