
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Versioning;

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
    private readonly Lock lockObject;
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

        ArgumentNullException.ThrowIfNull(outputFormat);
        ArgumentNullException.ThrowIfNull(providerToResample);

        lockObject = new();
        targetFormat = outputFormat;

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
    /// <exception cref="ObjectDisposedException">The native converter object has been disposed of.</exception>
    /// <exception cref="AudioConverterException">This property is not supported for this audio converter object.</exception>
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
    /// <exception cref="ObjectDisposedException">The native converter object has been disposed of.</exception>
    /// <exception cref="AudioConverterException">This property is not supported for this audio converter object.</exception>
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
    /// <exception cref="ObjectDisposedException">The native converter object has been disposed of.</exception>
    /// <exception cref="AudioConverterException">This property is not supported for this audio converter object.</exception>
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
    /// <exception cref="ObjectDisposedException">The native converter object has been disposed of.</exception>
    /// <exception cref="AudioConverterException">This property is not supported for this audio converter object.</exception>
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
    /// Allows to assign a custom channel mapping matrix to
    /// map an input channel to an output channel. <br />
    /// The array is arranged as follows: <br />
    /// Each element in the array is an output channel.
    /// The first element is the first channel, the second the second channel, and goes on.
    /// Each of these elements do contain an index to an index of a channel of the
    /// source <see cref="IWaveProvider"/> format. <br />
    ///
    /// An example (Stereo input to four channel output, the stereo input is copied to the last two channels):
    ///
    /// <code>
    /// int[] chMap = [ 0, 1, 0, 1 ];
    /// theConverter.SetChannelMap(chMap);
    /// </code>
    /// </summary>
    /// <remarks>
    /// You may also set any element to a value of <c>-1</c>.
    /// This special value means that no input channel is to be
    /// mapped to the output.
    /// </remarks>
    /// <param name="channelMap">
    /// The array that contains the channel map to assign.
    /// The length of this array must be the value of the <see cref="WaveFormat.Channels"/> property.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="channelMap"/> is <see langword="null"/>.</exception>
    /// <exception cref="AudioConverterException">If <paramref name="channelMap"/> is invalid for the current object or it cannot be set.</exception>
    /// <exception cref="ArgumentException"><paramref name="channelMap"/> must have a length equal to the output format number of channels.</exception>
    public void SetChannelMap(int[] channelMap)
    {
        ArgumentNullException.ThrowIfNull(channelMap);
        ObjectDisposedException.ThrowIf(actualConverter.IsDisposed, this);
        if (channelMap.Length == targetFormat.Channels)
        {
            actualConverter.SetChannelMap(channelMap);
        }
        else
        {
            throw new ArgumentException("The channel map array must have a length equal to the output number of channels.", nameof(channelMap));
        }
    }

    /// <summary>
    /// Resets the buffer state of the audio converter object,
    /// if there is reported a discontinuity in the source provider.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The native converter object has been disposed of.</exception>
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
        lockObject.Enter();
        try
        {
            if (!actualConverter.IsDisposed)
            {
                actualConverter.Dispose();
            }
        }
        finally
        {
            lockObject.Exit();
        }
    }
}
