
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Versioning;

using NAudio.Dmo;
using NAudio.Utils;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

/// <summary>
/// Provides the platform's resampler. <br />
/// Has almost the same strengths as the Windows Media Resampler (excluding changing the input provider's audio format details on the fly), 
/// and even provides more options compared to that one, allowing to select resampling algorithm, quality and dithering.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.2")]
public sealed unsafe class MacAudioConverter : IWaveProvider, IDisposable
{
    private readonly object lockObject;
    private readonly WaveFormat targetFormat;
    private readonly LowLevelAudioConverter actualConverter;

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

        ArgumentNullException.ThrowIfNull(targetFormat = outputFormat);
        ArgumentNullException.ThrowIfNull(providerToResample);

        lockObject = new();

        // Although that the converter can also work with compressed formats,
        // it will probably return the encoded data only and not any useful headers
        // that are required to read those specific formats.
        // Also, we just provide this for the resampling algorithm,
        // so it is probably OK to allow only PCM and IEEE floating-point formats.
        VerifyFormatIsIeeeFloatOrPCM(targetFormat);

        var sourceFormat = providerToResample.WaveFormat;

        // Construct the converter - translate the audio formats as needed.

        actualConverter = new(
            new(providerToResample.Read),
            MacUtils.ConstructASBDFromWaveFormat(sourceFormat),
            MacUtils.ConstructASBDFromWaveFormat(targetFormat)
        );

        try
        {
            // Convert, then assign the source provider format channel layout.
            if (sourceFormat is WaveFormatExtensible inExt && inExt.ChannelMask != 0)
            {
                var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)inExt.ChannelMask);

                actualConverter.AssignChannelLayout(
                    new(&l),
                    (uint)sizeof(AudioChannelLayout),
                    false
                );
            }

            // Convert, then assign the desired output format channel layout.
            if (targetFormat is WaveFormatExtensible outExt && outExt.ChannelMask != 0)
            {
                var l = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)outExt.ChannelMask);

                actualConverter.AssignChannelLayout(
                    new(&l),
                    (uint)sizeof(AudioChannelLayout),
                    true
                );
            }

            // Special case: If the source is single-channel but we want to resample to more than one channels,
            // change the channel map to provide the input to all the channels. By default, the resampler
            // provides the mono data to the first channel only, leaving all the others silent.
            if (sourceFormat.Channels == 1 && targetFormat.Channels > 1)
            {
                int[] chMap = new int[targetFormat.Channels];
                Array.Fill(chMap, 0);
                actualConverter.SetChannelMap(chMap);
            }

            // Initialize the native buffer, then we are ready to resample.
            actualConverter.InitializeNativeBuffer();
        }
        catch
        {
            actualConverter.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Provides the audio format under which the current resampler produces results as.
    /// </summary>
    public WaveFormat WaveFormat => targetFormat;

    /// <summary>
    /// Reads data out from the source provider,
    /// passing them through this configured resampler.
    /// </summary>
    /// <param name="buffer">The buffer to place resampled data into.</param>
    /// <returns>Number of bytes actually read into <paramref name="buffer"/>, 0 if end of stream.</returns>
    public int Read(Span<byte> buffer)
    {
        // mdcdi1315: I am considering of removing the below ThrowIf call to aid the audio thread to execute faster.
        ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
        if (buffer.Length < targetFormat.BlockAlign)
        {
            // We have a value less than BlockAlign.
            // Throw to avoid such subtle issues.
            throw new ArgumentException("Buffer length cannot be less than the stream's block alignment.", nameof(buffer));
        }
        else
        {
            return actualConverter.Read(buffer);
        }
    }

    /// <summary>
    /// Gets/sets the quality of the audio converter.
    /// </summary>
    public AudioConverterQuality Quality
    {
        get
        {
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            return actualConverter.Quality;
        }
        set
        {
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            actualConverter.Quality = value;
        }
    }

    /// <summary>
    /// Gets/sets the algorithm to use for resampling data.
    /// </summary>
    public AudioConverterSampleRateComplexity Complexity
    {
        get
        {
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            return actualConverter.Complexity;
        }
        set
        {
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            actualConverter.Complexity = value;
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
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            return actualConverter.Dithering;
        }
        set
        {
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            actualConverter.Dithering = value;
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
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            return actualConverter.DitheringBitLength;
        }
        set
        {
            ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
            actualConverter.DitheringBitLength = value;
        }
    }

    /// <summary>
    /// Resets the buffer state of the audio converter object,
    /// if there is reported a discontinuity in the source provider.
    /// </summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
        actualConverter.Reset();
    }

    /// <summary>
    /// Releases any resources used by this <see cref="MacAudioConverter"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        Monitor.Enter(lockObject);
        try
        {
            if (!actualConverter.IsDisposed)
            {
                actualConverter.Dispose();
            }
        }
        finally
        {
            Monitor.Exit(lockObject);
        }
    }
}