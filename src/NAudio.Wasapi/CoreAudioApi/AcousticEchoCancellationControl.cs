using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi;

/// <summary>
/// Controls the acoustic echo cancellation (AEC) reference stream for a capture
/// <see cref="AudioClient"/>. Wraps the Windows <c>IAcousticEchoCancellationControl</c> interface.
/// </summary>
/// <remarks>
/// <para>
/// AEC itself is performed by an audio processing object (APO) in the capture pipeline (supplied
/// by the device/driver or by Windows). NAudio does not implement echo cancellation — this class
/// only lets you nominate which render endpoint provides the loopback reference signal that the
/// AEC effect subtracts from the microphone input.
/// </para>
/// <para>
/// Obtain an instance from <see cref="AudioClient.TryGetAcousticEchoCancellationControl"/> after
/// the capture stream has been initialized. Requires Windows 11 build 22621 or later, and a
/// capture endpoint whose AEC effect supports control over the loopback reference endpoint.
/// </para>
/// </remarks>
public class AcousticEchoCancellationControl : IDisposable
{
    private IAcousticEchoCancellationControl acousticEchoCancellationControlInterface;

    internal AcousticEchoCancellationControl(IntPtr nativePointer)
    {
        try
        {
            acousticEchoCancellationControlInterface =
                (IAcousticEchoCancellationControl)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    nativePointer, CreateObjectFlags.UniqueInstance);
        }
        finally
        {
            Marshal.Release(nativePointer);
        }
    }

    /// <summary>
    /// Sets the render endpoint that should be used as the reference stream for AEC.
    /// </summary>
    /// <param name="renderEndpointId">The endpoint ID of the render endpoint (see
    /// <see cref="MMDevice.ID"/>), or null to let Windows pick the reference device automatically.</param>
    /// <exception cref="CoreAudioException">Thrown when the endpoint ID is invalid (E_INVALIDARG).</exception>
    public void SetReferenceEndpoint(string renderEndpointId)
    {
        CoreAudioException.ThrowIfFailed(
            acousticEchoCancellationControlInterface.SetEchoCancellationRenderEndpoint(renderEndpointId));
    }

    /// <summary>
    /// Sets the render device that should be used as the reference stream for AEC.
    /// </summary>
    /// <param name="renderDevice">The render device to use as the reference stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="renderDevice"/> is null.</exception>
    public void SetReferenceEndpoint(MMDevice renderDevice)
    {
        if (renderDevice == null)
        {
            throw new ArgumentNullException(nameof(renderDevice));
        }
        SetReferenceEndpoint(renderDevice.ID);
    }

    /// <summary>
    /// Resets the reference stream so Windows picks the loopback reference device itself,
    /// using its own algorithm (passes a null endpoint ID).
    /// </summary>
    public void UseDefaultReferenceEndpoint()
    {
        SetReferenceEndpoint((string)null);
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        if (acousticEchoCancellationControlInterface != null)
        {
            if ((object)acousticEchoCancellationControlInterface is ComObject co)
            {
                co.FinalRelease();
            }
            acousticEchoCancellationControlInterface = null;
        }
        GC.SuppressFinalize(this);
    }
}
