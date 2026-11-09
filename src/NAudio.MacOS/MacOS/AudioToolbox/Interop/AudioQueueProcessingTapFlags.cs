// This interop definition was derived from the file AudioQueue.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

namespace NAudio.MacOS.AudioToolbox.Interop;

/// <summary>
/// AudioQueueProcessingTapFlags <br />
/// Flags used in conjunction with processing taps
///
///	In the flags passed to AudioQueueProcessingTapNew, either the PreEffects
///	or PostEffects flag must be set, but not both. 
/// </summary>
internal enum AudioQueueProcessingTapFlags : uint
{
    //  these are flags that are passed to both the constructor and the callback

    /// <summary>
    /// Signifies that the processing tap is inserted before any effects.
    /// Passed to AudioQueueProcessingTapNew and to the callback.
    /// </summary>
    kAudioQueueProcessingTap_PreEffects         = 1 << 0,     // 0x01
    /// <summary>
    /// Signifies that the processing tap is inserted after any effects.
    /// Passed to AudioQueueProcessingTapNew and to the callback.
    /// </summary>
    kAudioQueueProcessingTap_PostEffects        = 1 << 1,     // 0x02
    /// <summary>
    /// Signifies that the processing tap is a siphon; it does not call
    /// GetSourceAudio. The callback instead receives the source audio
    /// and may not modify it. Passed to AudioQueueProcessingTapNew and to the callback.
    /// </summary>
    kAudioQueueProcessingTap_Siphon             = 1 << 2,     // 0x04

    //  these are flags that are passed to the callback and from GetSourceAudio

    /// <summary>
    /// Signifies that the source audio is the beginning of a continuous stream,
    /// i.e. following the beginning or resumption of playback or recording.
    /// Returned from GetSourceAudio.
    /// </summary>
    kAudioQueueProcessingTap_StartOfStream      = 1 << 8,     // 0x100
    /// <summary>
    /// Signifies that the source audio is past the end of stream. This happens when
    /// the audio queue is being stopped asynchronously and has finished playing
    /// all of its data. Returned from GetSourceAudio and should be propagated
    /// on return from the callback.
    /// </summary>
    kAudioQueueProcessingTap_EndOfStream        = 1 << 9,     // 0x200
}