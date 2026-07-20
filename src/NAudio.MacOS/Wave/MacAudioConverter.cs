
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.Dmo;
using NAudio.Utils;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.Wave;

/// <summary>
/// Provides the platform's resampler. <br />
/// Has almost the same strengths as the Windows Media Resampler, and even provides more
/// options compared to that one, allowing to modify resampling algorithm, quality and dithering.
/// </summary>
// mdcdi1315: TODO: This work is primitive but for now it works.
// Things I want to do:
// 1. The native callback be allocated as a private static field in the class - safe change.
// 2. Verify whether the converter is able to change the input stream format on the fly 
// - very important but also hard to test.
// 3. Check whether this does not leak any native objects, we are doing a lot of unsafe things in
// here - so this needs a verification.
public unsafe sealed class MacAudioConverter : IWaveProvider, IDisposable
{
    private WaveFormat sourceFormat;
    private readonly GCHandle selfGcHandle;
    private readonly WaveFormat outputFormat;
    private readonly IntPtr audioConverterHandle;
    private readonly IWaveProvider sourceProvider;

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void VerifyFormatIsIeeeFloatOrPCM(WaveFormat fmt)
    {
        if (fmt is WaveFormatExtensible extensible)
        {
            if (extensible.SubFormat != AudioMediaSubtypes.MEDIASUBTYPE_PCM &&
                extensible.SubFormat != AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT)
            {
                throw new ArgumentException("Format not IEEE floating-point or PCM");
            }
        }
        else if (fmt.Encoding != WaveFormatEncoding.Pcm &&
                fmt.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Format not IEEE floating-point or PCM");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MacAudioConverter"/> class,
    /// providing the source wave provider to resample, as well as the desired 
    /// format to convert the data of the provider into.
    /// </summary>
    /// <param name="providerToResample">The <see cref="IWaveProvider"/> whose data are subject to be resampled.</param>
    /// <param name="outputFormat">The audio format to output results as.</param>
    public MacAudioConverter(IWaveProvider providerToResample, WaveFormat outputFormat)
    {
        ArgumentNullException.ThrowIfNull(outputFormat);
        ArgumentNullException.ThrowIfNull(providerToResample);

        VerifyFormatIsIeeeFloatOrPCM(this.outputFormat = outputFormat);

        var sourceAsbd = MacUtils.ConstructASBDFromWaveFormat(sourceFormat = (sourceProvider = providerToResample).WaveFormat);
        var destAsbd = MacUtils.ConstructASBDFromWaveFormat(outputFormat);

        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterNew(
                sourceAsbd,
                destAsbd,
                out audioConverterHandle
            )
        );

        // Setup not complete yet - assign channel layouts as appropriate.

        if (outputFormat is WaveFormatExtensible ext)
        {
            AudioChannelLayout l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)ext.ChannelMask);

            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterInputChannelLayout,
                    (uint)sizeof(AudioChannelLayout),
                    new(&l)
                )
            );
        }

        UpdateSourceChannelLayout();

        selfGcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
    }

    private void UpdateSourceChannelLayout()
    {
        if (sourceFormat is WaveFormatExtensible ext)
        {
            AudioChannelLayout l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)ext.ChannelMask);

            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterOutputChannelLayout,
                    (uint)sizeof(AudioChannelLayout),
                    new(&l)
                )
            );
        }
    }

    private void UpdateSourceFormat()
    {
        var provFormat = sourceProvider.WaveFormat;
        if (!provFormat.Equals(sourceFormat))
        {
            var asbd = MacUtils.ConstructASBDFromWaveFormat(sourceFormat = provFormat);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterCurrentInputStreamDescription,
                    (uint)sizeof(AudioStreamBasicDescription),
                    new(&asbd)
                )
            );
            UpdateSourceChannelLayout();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int ConvertComplexBufferCb(
        System.IntPtr converter,
        uint* ioNumberDataPackets,
        nint ioData,
        nint outDataPacketDescription,
        nint inUserData
    )
    {
        var wrapper = (MacAudioConverter)GCHandle.FromIntPtr(inUserData).Target;

        uint bytesRead = 0U;
        bool updateSpan = true;
        Span<byte> currentSpan = Span<byte>.Empty;
        uint I = 0U, cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(ioData);
        do
        {
            if (updateSpan)
            {
                updateSpan = false;
                currentSpan = AudioBufferList.GetAudioBufferFromPointer(ioData, I).GetSpan();
            }
            int dataRead = wrapper.sourceProvider.Read(currentSpan);

            if (dataRead == 0)
            {
                break;
            }
            else if (dataRead == currentSpan.Length)
            {
                // It means that the current buffer was entirely consumed, move to the next one.
                updateSpan = true;
                I++;
            }
            else
            {
                // Continue attempting filling the span.
                currentSpan = currentSpan.Slice(dataRead);
            }

            bytesRead += (uint)dataRead;
        } while (I < cBuffers);

        *ioNumberDataPackets = MacUtils.GetNumberOfPacketsFromBytesAndFormat(bytesRead, wrapper.sourceFormat);

        return 0;
    }

    /// <summary>
    /// Provides the audio format under which the current resampler produces results as.
    /// </summary>
    public WaveFormat WaveFormat => outputFormat;

    /// <summary>
    /// Reads data out from the source provider,
    /// passing them through this configured resampler.
    /// </summary>
    /// <param name="buffer">The buffer to place resampled data into.</param>
    /// <returns>Number of bytes actually read into <paramref name="buffer"/>, 0 if end of stream.</returns>
    public int Read(Span<byte> buffer)
    {
        UpdateSourceFormat();
        uint readPackets = MacUtils.GetNumberOfPacketsFromBytesAndFormat((uint)buffer.Length, outputFormat);
        fixed (byte* pPlaceDataTo = buffer)
        {
            AudioBufferList list = new(new(new(pPlaceDataTo), (uint)buffer.Length, (uint)outputFormat.Channels));
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterFillComplexBuffer(
                    audioConverterHandle,
                    &ConvertComplexBufferCb,
                    GCHandle.ToIntPtr(selfGcHandle),
                    ref readPackets,
                    ref list,
                    IntPtr.Zero
                )
            );
        }
        return (int)MacUtils.GetNumberOfBytesFromPacketsAndFormat(readPackets, outputFormat);
    }

    /// <summary>
    /// Gets/sets the quality of the audio converter.
    /// </summary>
    public AudioConverterQuality Quality
    {
        get
        {
            ObjectDisposedException.ThrowIf(audioConverterHandle == IntPtr.Zero, this);
            AudioConverterQuality q;
            uint size = sizeof(AudioConverterQuality);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterGetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterSampleRateConverterQuality,
                    ref size,
                    new(&q)
                )
            );
            return q;
        }
        set
        {
            ObjectDisposedException.ThrowIf(audioConverterHandle == IntPtr.Zero, this);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterSampleRateConverterQuality,
                    sizeof(AudioConverterQuality),
                    new(&value)
                )
            );
        }
    }

    /// <summary>
    /// Gets/sets the algorithm to use for resampling data.
    /// </summary>
    public AudioConverterSampleRateComplexity Complexity
    {
        get
        {
            ObjectDisposedException.ThrowIf(audioConverterHandle == IntPtr.Zero, this);
            AudioConverterSampleRateComplexity c;
            uint size = sizeof(AudioConverterSampleRateComplexity);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterGetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterSampleRateConverterComplexity,
                    ref size,
                    new(&c)
                )
            );
            return c;
        }
        set
        {
            ObjectDisposedException.ThrowIf(audioConverterHandle == IntPtr.Zero, this);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterSampleRateConverterComplexity,
                    sizeof(AudioConverterQuality),
                    new(&value)
                )
            );
        }
    }

    /// <summary>
    /// Resets the buffer state of the audio converter object,
    /// if there is reported a discontinuity in the source provider.
    /// </summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(audioConverterHandle == IntPtr.Zero, this);
        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterReset(audioConverterHandle)
        );
    }

    /// <summary>
    /// Releases any resources used by this <see cref="MacAudioConverter"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        Monitor.Enter(this);
        try
        {
            if (selfGcHandle.IsAllocated)
            {
                try
                {
                    AudioConverterException.ThrowIfError(
                        NativeMethods.AudioConverterDispose(audioConverterHandle)
                    );
                }
                finally
                {
                    selfGcHandle.Free();
                }
            }
        }
        finally
        {
            Monitor.Exit(this);
        }
    }
}