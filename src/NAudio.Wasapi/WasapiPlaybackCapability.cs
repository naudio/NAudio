using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace NAudio.Wave;

/// <summary>
/// Describes what <see cref="WasapiPlayer.Init"/> would do for a particular source format, as returned
/// by <see cref="WasapiPlayer.GetPlaybackCapability"/>. Lets you validate a chosen configuration
/// before opening the audio stream.
/// </summary>
/// <param name="Supported">
/// Whether playback is possible at all. False only in exclusive mode when the device cannot accept the
/// source's sample rate (which would require resampling); shared mode is always supported.
/// </param>
/// <param name="ShareMode">The share mode that would be used.</param>
/// <param name="LowLatencyActive">
/// Whether the IAudioClient3 low-latency path would actually engage. Only ever true when low latency
/// was requested and the source can be adapted to the engine mix format without resampling.
/// </param>
/// <param name="OutputFormat">The format the device would receive (after any conversion).</param>
/// <param name="Conversions">
/// Human-readable descriptions of the latency-free conversions that would be inserted (bit depth,
/// channels), or empty when the source is used as-is. Note that in standard shared mode the audio
/// engine performs conversion (including sample-rate) itself, so this list is empty there.
/// </param>
/// <param name="LatencyMilliseconds">The latency that would be used, in milliseconds.</param>
/// <param name="Reason">
/// When <paramref name="Supported"/> is false, why playback is not possible. When low latency was
/// requested but <paramref name="LowLatencyActive"/> is false, why it could not be honoured. Null
/// otherwise.
/// </param>
public sealed record WasapiPlaybackCapability(
    bool Supported,
    AudioClientShareMode ShareMode,
    bool LowLatencyActive,
    WaveFormat OutputFormat,
    IReadOnlyList<string> Conversions,
    int LatencyMilliseconds,
    string Reason);
