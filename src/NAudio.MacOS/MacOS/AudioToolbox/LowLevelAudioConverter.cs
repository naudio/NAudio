
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

// A low-level basic layout of the macOS audio converter.
// This was created to commonize the code that will be required
// to translate formats to the Core Audio player and recorder.
// It will also be used by the exported resampler.
//
// mdcdi1315: TODO: This work is primitive but for now it works.
// Things I want to do:
// 1. Probably we cannot change the input stream properties once the converter is initialized,
// there are some properties that somehow allow to modify sample rate, but are a bit misleading on
// which end they are applied to - worth checking, especially if will be able to support providers 
// that do change audio formats.
// 2. Check whether this does not leak any native objects, we are doing a lot of unsafe things in
// here - so this needs a verification.
internal unsafe sealed class LowLevelAudioConverter : IDisposable
{
    // Allocate a single function pointer for all convert complex buffer calls - 
    // the same things are done to all the audio converter wrapper objects.
    private static readonly AudioConverterComplexInputDataProc procedureInstance = &ConvertComplexBufferCb;

    private byte* nativeBuffer;
    private GCHandle selfGcHandle;
    private uint nativeBufferSize;
    private readonly IntPtr audioConverterHandle;
    private bool disposed, requiresFillComplexBuffer;
    private Exception exceptionOnFillComplexBufferCb;
    private readonly ProviderReadDelegate readDelegate;
    public readonly AudioStreamBasicDescription sourceFormat;
    public readonly AudioStreamBasicDescription outputFormat;

    public delegate int ProviderReadDelegate(Span<byte> buffer);

    // Constructs a new instance of the low-level audio converter.
    // Provide the read delegate as well as the descriptions for the 
    // data provided by the 'read' delegate as well as the desired target format.
    public LowLevelAudioConverter(
        ProviderReadDelegate readDelegate,
        AudioStreamBasicDescription sourceFormat,
        AudioStreamBasicDescription outputFormat
    )
    {
        ArgumentNullException.ThrowIfNull(readDelegate);
        this.readDelegate = readDelegate;
        this.sourceFormat = sourceFormat;
        this.outputFormat = outputFormat;

        disposed = false;
        requiresFillComplexBuffer = false;

        // Construct the converter - provide the source and output formats to it.

        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterNew(
                sourceFormat,
                outputFormat,
                out audioConverterHandle
            )
        );
    }

    public void InitializeNativeBuffer()
    {
        // We need to change the conversion function we use
        // when no sample rate conversion is used.
        if (sourceFormat.mSampleRate == outputFormat.mSampleRate)
        {
            // Query the input minimum buffer size, and use that as the native intermediate buffer.
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

            selfGcHandle = default;
            requiresFillComplexBuffer = false;
        }
        else
        {
            nativeBufferSize = sourceFormat.ConvertLatencyToByteSize(100);
            nativeBuffer = (byte*)NativeMemory.Alloc(nativeBufferSize);

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

    public void AssignChannelLayout(IntPtr pointerToLayout, uint pointerSize, bool isOutput)
    {
        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterSetProperty(
                audioConverterHandle,
                isOutput ?
                    AudioConverterProperties.kAudioConverterOutputChannelLayout :
                    AudioConverterProperties.kAudioConverterInputChannelLayout,
                pointerSize,
                pointerToLayout
            )
        );
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
        var wrapper = (LowLevelAudioConverter)GCHandle.FromIntPtr(inUserData).Target;

        AudioBufferList.SetNumberOfBuffersFromPointer(ioData, 1U);
        AudioBuffer buffer = new(
            new(wrapper.nativeBuffer),
            wrapper.nativeBufferSize,
            wrapper.sourceFormat.mChannelsPerFrame
        );
        AudioBufferList.SetAudioBufferFromPointer(ioData, 0U, buffer);

        uint bytesRead = 0U;
        Span<byte> currentSpan = buffer.GetSpan();
        while (currentSpan.Length > 0)
        {
            int dataRead;
            try
            {
                dataRead = wrapper.readDelegate.Invoke(currentSpan);
            }
            catch (Exception ex)
            {
                // We can't throw wave provider errors here - capture the exception to throw it back to the Read call.
                dataRead = 0;
                wrapper.exceptionOnFillComplexBufferCb = ex;
                break;
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
            else
            {
                // Continue attempting filling the span.
                currentSpan = currentSpan.Slice(dataRead);
            }

            bytesRead += (uint)dataRead;
        }

        *ioNumberDataPackets = bytesRead / wrapper.sourceFormat.mBytesPerFrame;

        return 0;
    }

    public bool IsDisposed => disposed;

    public int Read(Span<byte> buffer)
    {
        uint bufferLength = (uint)buffer.Length;
        if (requiresFillComplexBuffer)
        {
            uint readPackets = bufferLength / outputFormat.mBytesPerFrame;
            fixed (byte* pPlaceDataTo = buffer)
            {
                var list = AudioBufferList.FromSingleBuffer(new(pPlaceDataTo), bufferLength, outputFormat.mChannelsPerFrame);
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
                return (int)(readPackets * outputFormat.mBytesPerFrame);
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
            int readBytes = readDelegate.Invoke(new Span<byte>(nativeBuffer, (int)nativeBufferSize));
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

    public void Reset()
    {
        AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterReset(audioConverterHandle)
        );
    }

    public AudioConverterQuality Quality
    {
        get
        {
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
        set => AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterSetProperty(
                audioConverterHandle,
                AudioConverterProperties.kAudioConverterSampleRateConverterQuality,
                sizeof(AudioConverterQuality),
                new(&value)
            )
        );
    }

    public AudioConverterSampleRateComplexity Complexity
    {
        get
        {
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
        set => AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterSetProperty(
                audioConverterHandle,
                AudioConverterProperties.kAudioConverterSampleRateConverterComplexity,
                sizeof(AudioConverterSampleRateComplexity),
                new(&value)
            )
        );
    }

    public AudioConverterDitheringAlgorithm Dithering
    {
        get
        {
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
        set => AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterSetProperty(
                audioConverterHandle,
                AudioConverterProperties.kAudioConverterPropertyDithering,
                sizeof(AudioConverterDitheringAlgorithm),
                new(&value)
            )
        );
    }

    public uint DitheringBitLength
    {
        get
        {
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

        set => AudioConverterException.ThrowIfError(
            NativeMethods.AudioConverterSetProperty(
                audioConverterHandle,
                AudioConverterProperties.kAudioConverterPropertyDitherBitDepth,
                sizeof(AudioConverterDitheringAlgorithm),
                new(&value)
            )
        );
    }

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