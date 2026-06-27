using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces;

/// <summary>
/// Windows CoreAudio IAcousticEchoCancellationControl interface.
/// Defined in AudioClient.h. Provides a mechanism for setting the audio render endpoint
/// that should be used as the reference stream for acoustic echo cancellation (AEC).
/// Requires Windows 11 build 22621 or later.
/// </summary>
[GeneratedComInterface]
[Guid("f4ae25b5-aaa3-437d-b6b3-dbbe2d0e9549")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IAcousticEchoCancellationControl
{
    /// <summary>
    /// Sets the audio render endpoint that should be used as the reference stream for AEC.
    /// Passing null lets Windows pick the loopback reference device using its own algorithm.
    /// </summary>
    /// <param name="endpointId">The endpoint ID of the render endpoint to use as the reference
    /// stream, or null to let Windows choose. An invalid render device ID fails with E_INVALIDARG.</param>
    [PreserveSig]
    int SetEchoCancellationRenderEndpoint([MarshalAs(UnmanagedType.LPWStr)] string endpointId);
}
