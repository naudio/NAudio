
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Versioning;
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
/// Has almost the same strengths as the Windows Media Resampler (excluding changing the input provider's audio format details on the fly), 
/// and even provides more options compared to that one, allowing to select resampling algorithm, quality and dithering.
/// </summary>
// mdcdi1315: TODO: This work is primitive but for now it works.
// Things I want to do:
// 1. Probably we cannot change the input stream properties once the converter is initialized,
// there are some properties that somehow allow to modify sample rate, but are a bit misleading on
// which end they are applied to - worth checking, especially if will be able to support providers 
// that do change audio formats.
// 2. Check whether this does not leak any native objects, we are doing a lot of unsafe things in
// here - so this needs a verification.
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.2")]
public sealed unsafe class MacAudioConverter : IWaveProvider, IDisposable
{
    // Allocate a single function pointer for all convert complex buffer calls - 
    // the same things are done to all the audio converter wrapper objects.
    private static readonly AudioConverterComplexInputDataProc procedureInstance = &ConvertComplexBufferCb;

    private byte* nativeBuffer;
    private GCHandle selfGcHandle;
    private readonly uint nativeBufferSize;
    private readonly WaveFormat sourceFormat;
    private readonly WaveFormat outputFormat;
    private readonly IntPtr audioConverterHandle;
    private readonly IWaveProvider sourceProvider;
    private bool disposed, requiresFillComplexBuffer;
    private Exception exceptionOnFillComplexBufferCb;

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
        VersioningVerifier.VerifyWeAreInSupportedVersion();

        ArgumentNullException.ThrowIfNull(outputFormat);
        ArgumentNullException.ThrowIfNull(providerToResample);

        disposed = false;
        requiresFillComplexBuffer = false;

        // Although that the converter can also work with compressed formats,
        // it will probably return the encoded data only and not any useful headers
        // that are required to read those specific formats.
        // Also, we just provide this for the resampling algorithm,
        // so it is probably OK to allow only PCM and IEEE floating-point formats.
        VerifyFormatIsIeeeFloatOrPCM(this.outputFormat = outputFormat);

        // Construct the converter - translate the audio formats as needed.

        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterNew(
                MacUtils.ConstructASBDFromWaveFormat(sourceFormat = (sourceProvider = providerToResample).WaveFormat),
                MacUtils.ConstructASBDFromWaveFormat(outputFormat),
                out audioConverterHandle
            )
        );

        if (outputFormat is WaveFormatExtensible outExt && outExt.ChannelMask != 0)
        {
            var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)outExt.ChannelMask);

            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterOutputChannelLayout,
                    (uint)sizeof(AudioChannelLayout),
                    new(&l)
                )
            );
        }

        try
        {
            InitCommon();
        }
        catch
        {
            _ = NativeMethods.AudioConverterDispose(audioConverterHandle);
            throw;
        }
    }

    internal MacAudioConverter(
        IWaveProvider providerToResample,
        AudioStreamBasicDescription targetFormat,
        IntPtr targetChannelLayoutPointer
    )
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();

        ArgumentNullException.ThrowIfNull(providerToResample);

        disposed = false;
        requiresFillComplexBuffer = false;

        // Construct the converter - translate the audio formats as needed.

        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterNew(
                MacUtils.ConstructASBDFromWaveFormat(sourceFormat = (sourceProvider = providerToResample).WaveFormat),
                targetFormat,
                out audioConverterHandle
            )
        );

        outputFormat = MacUtils.ConstructWaveFormatFromASBD(targetFormat);

        if (targetChannelLayoutPointer != IntPtr.Zero)
        {
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterOutputChannelLayout,
                    (uint)sizeof(AudioChannelLayout),
                    targetChannelLayoutPointer
                )
            );
        }

        try
        {
            InitCommon();
        }
        catch
        {
            _ = NativeMethods.AudioConverterDispose(audioConverterHandle);
            throw;
        }
    }

    private void InitCommon()
    {
        // Query the input mimimum buffer size, and use that as the native intermediate buffer.
        fixed (uint* nativeBufferSizePointer = &nativeBufferSize)
        {
            uint size = sizeof(uint);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterGetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterPropertyMinimumInputBufferSize,
                    ref size,
                    new(nativeBufferSizePointer)
                )
            );
        }
        nativeBuffer = (byte*)NativeMemory.Alloc(nativeBufferSize);

        // Assign channel layout of the source format.
        if (sourceFormat is WaveFormatExtensible inExt && inExt.ChannelMask != 0)
        {
            var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)inExt.ChannelMask);

            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterInputChannelLayout,
                    (uint)sizeof(AudioChannelLayout),
                    new(&l)
                )
            );
        }

        // We need to change the conversion function we use
        // when no sample rate conversion is used.
        if (sourceFormat.SampleRate == outputFormat.SampleRate)
        {
            selfGcHandle = default;
            requiresFillComplexBuffer = false;
        }
        else
        {
            requiresFillComplexBuffer = true;

            // Now allocate the GC handle. Allocating this earlier could
            // leak this handle. So, do whatever setup we intend to do first,
            // then we can initialize this safely. The only case for this to throw
            // is if we are out of memory, which may terminate the process,
            // so we might be safe. However, it is useful to verify that in the future,
            // because there are also recoverable out of memory situations, if the runtime
            // somehow manages to free memory.
            selfGcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int ConvertComplexBufferCb(
        nint converter,
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
        for (uint I = 0U, cBuffers = AudioBufferList.GetNumberOfBuffersFromPointer(ioData); I < cBuffers;) // We might have zero buffers to process
        {
            if (updateSpan)
            {
                updateSpan = false;
                currentSpan = AudioBufferList.GetAudioBufferFromPointer(ioData, I).GetSpan();
            }
            int dataRead;
            try
            {
                dataRead = wrapper.sourceProvider.Read(currentSpan);
            }
            catch (Exception ex)
            {
                // We can't throw wave provider errors here - capture the exception to throw it back to the Read call.
                dataRead = 0;
                wrapper.exceptionOnFillComplexBufferCb = ex;
            }

            if (dataRead == 0)
            {
                // Exit the loop - do not fill any additional data.
                // This will force assigning the number of packets
                // that we did actually read from the stream, 
                // which if it is zero, it will assign zero,
                // and that will flag our converter that we 
                // do not have any further data to process.
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
        }

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
        uint bufferLength = (uint)buffer.Length;
        if (requiresFillComplexBuffer)
        {
            uint readPackets = MacUtils.GetNumberOfPacketsFromBytesAndFormat(bufferLength, outputFormat);
            fixed (byte* pPlaceDataTo = buffer)
            {
                var list = AudioBufferList.FromSingleBuffer(new(pPlaceDataTo), bufferLength, (uint)outputFormat.Channels);
                AudioConverterException.ThrowIfError(
                    NativeMethods.AudioConverterFillComplexBuffer(
                        audioConverterHandle,
                        procedureInstance,
                        GCHandle.ToIntPtr(selfGcHandle),
                        ref readPackets,
                        ref list,
                        IntPtr.Zero
                    )
                );
            }

            if (exceptionOnFillComplexBufferCb is null)
            {
                return (int)MacUtils.GetNumberOfBytesFromPacketsAndFormat(readPackets, outputFormat);
            }
            else
            {
                var localException = exceptionOnFillComplexBufferCb;
                exceptionOnFillComplexBufferCb = null;
                throw localException;
            }
        }
        else
        {
            int readBytes = sourceProvider.Read(new Span<byte>(nativeBuffer, (int)nativeBufferSize));
            fixed (byte* pDstBuffer = buffer)
            {
                AudioConverterException.ThrowIfError(
                    NativeMethods.AudioConverterConvertBuffer(
                        audioConverterHandle,
                        (uint)readBytes,
                        new(nativeBuffer),
                        ref bufferLength,
                        new(pDstBuffer)
                    )
                );
            }
            return (int)bufferLength;
        }
    }

    /// <summary>
    /// Gets/sets the quality of the audio converter.
    /// </summary>
    public AudioConverterQuality Quality
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
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
            ObjectDisposedException.ThrowIf(disposed, this);
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
            ObjectDisposedException.ThrowIf(disposed, this);
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
            ObjectDisposedException.ThrowIf(disposed, this);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterSampleRateConverterComplexity,
                    sizeof(AudioConverterSampleRateComplexity),
                    new(&value)
                )
            );
        }
    }

    /// <summary>
    /// Gets/sets the dithering algorithm to apply to the audio converter. <br />
    /// The constant <see cref="AudioConverterDitheringAlgorithm.None"/> can be used to disable dithering. <br />
    /// This is only supported in macOS.
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    public AudioConverterDitheringAlgorithm Dithering
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            AudioConverterDitheringAlgorithm c;
            uint size = sizeof(AudioConverterDitheringAlgorithm);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterGetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterPropertyDithering,
                    ref size,
                    new(&c)
                )
            );
            return c;
        }
        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterPropertyDithering,
                    sizeof(AudioConverterDitheringAlgorithm),
                    new(&value)
                )
            );
        }
    }

    /// <summary>
    /// The pre-specified dithering algorithm is applied to the bit length denoted by the value of this property. <br />
    /// This is only supported in macOS.
    /// </summary>
    [UnsupportedOSPlatform("ios")]
    public uint DitheringBitLength
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            uint c;
            uint size = sizeof(uint);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterGetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterPropertyDitherBitDepth,
                    ref size,
                    new(&c)
                )
            );
            return c;
        }
        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            AudioConverterException.ThrowIfError(
                NativeMethods.AudioConverterSetProperty(
                    audioConverterHandle,
                    AudioConverterProperties.kAudioConverterPropertyDitherBitDepth,
                    sizeof(AudioConverterDitheringAlgorithm),
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
            if (disposed) { return; }
            try
            {
                AudioConverterException.ThrowIfError(
                    NativeMethods.AudioConverterDispose(audioConverterHandle)
                );
            }
            finally
            {
                if (nativeBuffer is not null)
                {
                    NativeMemory.Free(nativeBuffer);
                    nativeBuffer = null;
                }
                if (selfGcHandle.IsAllocated) { selfGcHandle.Free(); }
                disposed = true;
            }
        }
        finally
        {
            Monitor.Exit(this);
        }
    }
}