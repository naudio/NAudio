
using System;

using NAudio.Dmo;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

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
            FirePlaybackStopped(null, new(
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
            FirePlaybackStopped(this, new(ex));
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

    // Performs the actual initialization of the player.
    private void Initialize()
    {
        // Clear the player's interleaved flag.
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
                // We have non-interleaved case, we will need the resampler anyway.
                requiresResampler = true;
                // Modify the channel count so that the resampler
                // can correctly return non-interleaved samples
                asbdToUse.mChannelsPerFrame = (uint)streams.Length;
                // Add the non-interleaved flag if we detected the non-interleaved case
                // by seeing the number of streams.
                asbdToUse.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsNonInterleaved;
                flags |= CoreAudioPlayerStateFlags.NonInterleaved;
                break;
            }
            else if (
                asbdToUse.mSampleRate == providerFormat.SampleRate &&
                asbdToUse.mBitsPerChannel == providerFormat.BitsPerSample &&
                asbdToUse.mBytesPerFrame == providerFormat.BlockAlign &&
                asbdToUse.mChannelsPerFrame == providerFormat.Channels &&
                ProviderFormatEncodingMatchesPlayerFormat(asbdToUse, providerFormat)
            )
            {
                selectedStream = I;
                break;
            }
        }

        if (HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved))
        {
            // Non-interleaved case.
            // In this case, we require all the virtual formats to be the same.
            // If not the same, the resampler will NOT produce correct results,
            // and that's an issue. If the user wants to use that device,
            // he should apply the same virtual format in all the streams and 
            // retry.
            foreach (var s in streams)
            {
                var streamAsbd = s.VirtualFormatNative;
                if (
                    asbdToUse.mSampleRate != streamAsbd.mSampleRate ||
                    asbdToUse.mBitsPerChannel != streamAsbd.mBitsPerChannel ||
                    asbdToUse.mFormatFlags != streamAsbd.mFormatFlags
                )
                {
                    throw new InvalidOperationException("Non-interleaved audio device with different audio formats is not supported");
                }
            }
        }
        else if (asbdToUse.mFormatID == 0 || selectedStream < 0)
        {
            // A stream was not specified.
            // This means that we are interleaved case with resampler.
            // We will pick the first stream, as a result.
            selectedStream = 0;
            requiresResampler = true;
            asbdToUse = streams[0].VirtualFormatNative;
        }

        if (!requiresResampler)
        {
            // somehow, we do not need the resampler, however we might be in multi-channel
            // and so we need channel translation, and if we need translation, we need the resampler.
            _ = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output, out requiresResampler, out _);
        }

        if (requiresResampler)
        {
            using var clh = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output);
            // Configure the resampler as appropriate.
            selectedSource = new ResamplerSource(
                originalProvider,
                asbdToUse,
                clh
            );
        }
        else
        {
            // Otherwise, just use our wave provider directly.
            selectedSource = new RawSource(originalProvider, asbdToUse);
        }

        try
        {
            ioProcedure ??= new(selectedDevice);
            ioProcedure.Source = selectedSource;
            ioProcedure.PlaybackStopped += FirePlaybackStopped;
            // Now, enable only the stream we need.
            bool[] enabledStreams = new bool[streams.Length];
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved))
            {
                // Non-interleaved case
                for (int I = 0; I < streams.Length; I++)
                {
                    enabledStreams[I] = true;
                }
            }
            else
            {
                // Interleaved case
                for (int I = 0; I < streams.Length; I++)
                {
                    enabledStreams[I] = I == selectedStream;
                }
            }
            selectedDevice.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Output, enabledStreams);

            virtFormatChanged?.Dispose();
            virtFormatChanged = streams[HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved) ? 0 : selectedStream].ConstructVirtualFormatChangedEvent();
            virtFormatChanged.Event += OnVirtualFormatChanged;

            // Finally, flag our player as completed initialzation.
            OnInitializationComplete();
        }
        catch
        {
            // Undo all of our progress so far if we do not made it.
            selectedSource.Dispose();
            throw;
        }
    }
}