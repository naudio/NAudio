
using System;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

// Provides the management and initialization code of the player. 
// Anything that manages the player and the I/O procedure should be here.
public partial class CoreAudioPlayer
{
    private PropertyListenerHandle virtFormatChanged;

    /// <summary>
    /// Provides the actions to perform when an audio 
    /// device has been destroyed and will go away.
    /// </summary>
    /// <param name="audioDevice">The destroyed device object.</param>
    private void OnDeviceWillBeDestroyed(AudioObject audioDevice)
    {
        flags |= CoreAudioPlayerStateFlags.Invalidated;

        // If we have an I/O procedure attached, do the following.
        if (ioProcedure is not null && ioProcedure.IsRunning)
        {
            // Now, dispatch the playback stopped event with the event args containing
            // a Core Audio exception saying the device is no longer available.
            FirePlaybackStopped(new(
                new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError)
            ));
        }
    }

    /// <summary>
    /// Provides the actions to perform when the audio 
    /// device streams have been changed.
    /// </summary>
    /// <param name="audioDevice">The audio device object.</param>
    private void OnStreamsChanged(AudioObject audioDevice)
    {
        bool wasRunning = ioProcedure.IsRunning;

        ioProcedure.Stop();

        selectedSource.Dispose();

        Initialize();

        if (wasRunning) { ioProcedure.Start(); }
    }

    private void OnVirtualFormatChanged(AudioObject stream)
    {
        bool wasRunning = ioProcedure.IsRunning;
        ioProcedure.Stop();
        // Do not fire playback stopped event here.
        // Dispose the resampler, if we have one.
        selectedSource.Dispose();

        try
        {
            // Perform initialization from the beginning.
            Initialize();
            if (wasRunning) { ioProcedure.Start(); }
        }
        catch (Exception ex)
        {
            FirePlaybackStopped(new(ex));
        }
    }

    private bool ProviderFormatEncodingMatchesPlayerFormat(AudioStreamBasicDescription desc, WaveFormat providerFormat)
    {
        if (providerFormat is WaveFormatExtensible ext)
        {
            return ext.SubFormat == (
                desc.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsFloat)
                    ? AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT
                    : AudioMediaSubtypes.MEDIASUBTYPE_PCM
            );
        }
        else
        {
            return providerFormat.Encoding == (
                desc.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsFloat)
                    ? WaveFormatEncoding.IeeeFloat
                    : WaveFormatEncoding.Pcm
            );
        }
    }

    // Tests whether an ASBD coming from HAL matches the given WaveFormat.
    // This tests only the encodings and the common fields - sample rate,
    // bit rate, block align and number of channels.
    private bool ProviderFormatMatchesPlayerFormat(AudioStreamBasicDescription desc, WaveFormat providerFormat)
    {
        return ProviderFormatEncodingMatchesPlayerFormat(desc, providerFormat) &&
            desc.mSampleRate == providerFormat.SampleRate &&
            desc.mBitsPerChannel == providerFormat.BitsPerSample &&
            desc.mBytesPerFrame == providerFormat.BlockAlign &&
            desc.mChannelsPerFrame == providerFormat.Channels
        ;
    }

    // Performs the actual initialization of the player.
    private void Initialize()
    {
        // Clear the player's non-interleaved flag.
        flags &= ~CoreAudioPlayerStateFlags.NonInterleaved;
        // Get the streams.
        AudioStream[] streams = selectedDevice.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length == 0)
        {
            throw new InvalidOperationException("Could not find any stream to render to");
        }
        // Find & select a stream that has the best format for the provided format.
        var providerFormat = originalProvider.WaveFormat;

        int selectedStream = -1;
        bool requiresResampler = false;
        AudioStreamBasicDescription asbdToUse = default;

        for (int I = 0; I < streams.Length; I++)
        {
            asbdToUse = streams[I].VirtualFormatNative;
            if (asbdToUse.mFormatID != AudioFormatIDs.kAudioFormatLinearPCM)
            {
                // Not supported, see whether we have any other streams
                continue;
            }
            else if (
                asbdToUse.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved) ||
                (asbdToUse.mChannelsPerFrame == 1U && streams.Length > 1)
            )
            {
                // We are in non-interleaved case.
                // Add the non-interleaved flag.
                flags |= CoreAudioPlayerStateFlags.NonInterleaved;
                break;
            }
            else if (
                ProviderFormatMatchesPlayerFormat(asbdToUse, providerFormat)
            )
            {
                selectedStream = I;
                break;
            }
        }

        uint reportedLatency = 0U;
        foreach (var s in streams) { reportedLatency += s.Latency; }

        if (HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved))
        {
            // Non-interleaved case.
            // In this case, we require all the virtual formats to be the same.
            // If not the same, the deinterleaving algorithm will NOT produce 
            // correct results, and that's an issue. If the user wants to
            // use that device, he should apply the same virtual format 
            // in all the streams and retry.
            //
            // During this enumeration, we also build the channel count.
            uint channelCount = 0U;
            foreach (var s in streams)
            {
                var streamAsbd = s.VirtualFormatNative;
                channelCount += streamAsbd.mChannelsPerFrame;
                if (
                    asbdToUse.mSampleRate != streamAsbd.mSampleRate ||
                    asbdToUse.mBitsPerChannel != streamAsbd.mBitsPerChannel ||
                    asbdToUse.mFormatFlags != streamAsbd.mFormatFlags ||
                    asbdToUse.mChannelsPerFrame != streamAsbd.mChannelsPerFrame ||
                    asbdToUse.mBytesPerFrame != streamAsbd.mBytesPerFrame
                )
                {
                    throw new InvalidOperationException("Non-interleaved audio device with different audio formats is not supported");
                }
            }
            // Compute the number of bytes required for each channel.
            uint perChannelBytes = asbdToUse.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved)
                ? asbdToUse.mBytesPerFrame
                : asbdToUse.mBytesPerFrame / channelCount;
            var newAsbd = new AudioStreamBasicDescription()
            {
                mBitsPerChannel = asbdToUse.mBitsPerChannel,
                mChannelsPerFrame = channelCount,
                mFormatFlags = asbdToUse.mFormatFlags & ~AudioFormatFlags.kAudioFormatFlagIsNonInterleaved,
                mFramesPerPacket = 1U,
                mFormatID = AudioFormatIDs.kAudioFormatLinearPCM,
                mSampleRate = asbdToUse.mSampleRate
            };
            newAsbd.mBytesPerFrame = newAsbd.mBytesPerPacket = perChannelBytes * channelCount;
            asbdToUse = newAsbd;
            // If the formats still not match (which will typically be the case), we need the resampler.
            requiresResampler = !ProviderFormatMatchesPlayerFormat(asbdToUse, providerFormat);
        }
        else if (asbdToUse.mFormatID == 0 || selectedStream < 0)
        {
            // A stream was not specified.
            // This means that we are interleaved case with resampler.
            // We will pick the first stream, as a result.
            requiresResampler = true;
            asbdToUse = streams[selectedStream = 0].VirtualFormatNative;
        }

        if (!requiresResampler)
        {
            // somehow, we do not need the resampler, however we might be in multi-channel
            // and so we need channel translation, and if we need translation, we need the resampler.
            _ = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output, out requiresResampler, out _);
        }

        // The latency of the streams may report as zero - 
        // in such case, use the buffer frame size divided by the sample rate.
        if (reportedLatency == 0U)
        {
            reportedLatency = (uint)(selectedDevice.BufferFrameSize / asbdToUse.mSampleRate);
        }

        // Configure the player source. If we need the resampler,
        // we inject the resampler and pass the provider through it.
        if (requiresResampler)
        {
            using var clh = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output);
            // Configure the resampler as appropriate.
            selectedSource = new ResamplerSource(
                originalProvider,
                asbdToUse,
                clh,
                reportedLatency
            );
        }
        else
        {
            // Otherwise, just use our wave provider directly.
            selectedSource = new RawSource(originalProvider, asbdToUse, reportedLatency);
        }

        try
        {
            // Release any previously allocated I/O procedure.
            ioProcedure?.Dispose();
            // Now, enable only the stream(s) we need.
            bool[] enabledStreams = new bool[streams.Length];
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved))
            {
                // Non-interleaved case
                ioProcedure = new NonInterleavedProcedure(selectedDevice);
                Array.Fill(enabledStreams, true);
            }
            else
            {
                // Interleaved case
                ioProcedure = new InterleavedProcedure(selectedDevice);
                for (int I = 0; I < streams.Length; I++)
                {
                    enabledStreams[I] = I == selectedStream;
                }
            }
            ioProcedure.Source = selectedSource;
            ioProcedure.PlaybackStopped += FirePlaybackStopped;
            selectedDevice.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Output, enabledStreams);

            virtFormatChanged?.Dispose();
            virtFormatChanged = streams[HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved) ? 0 : selectedStream].ConstructVirtualFormatChangedEvent();
            virtFormatChanged.Event += OnVirtualFormatChanged;

            // Finally, flag our player as completed initialzation.
            OnInitializationComplete();
        }
        catch
        {
            // Undo all of our progress so far if we did not made it.
            MacUtils.EnsureDisposableObjectsDisposed(ioProcedure, virtFormatChanged, selectedSource);
            throw;
        }
    }
}