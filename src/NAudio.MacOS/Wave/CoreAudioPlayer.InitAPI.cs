
using System;
using System.Diagnostics;

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
    private void Initialize(bool isInit = false)
    {
        // Initial variables
        int selectedStream = -1;
        bool requiresResampler = false;
        var providerFormat = originalProvider.WaveFormat;
        // Clear the player's non-interleaved flag.
        flags &= ~CoreAudioPlayerStateFlags.NonInterleaved;
        // Get the streams.
        AudioStream[] streams = selectedDevice.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length == 0)
        {
            throw new InvalidOperationException("Could not find any stream to render to");
        }

        // Find & select a stream that has the best format for the provided format.
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

        /*
        // mdcdi1315: This is for exclusive mode support, but it seems that does not work
        // in the way we expect to, so probably this is not needed.

        // If Exclusive mode is applied to the device,
        // we can try specifying the format of the provider directly.
        if (selectedDevice.HogMode && TryConstructingExclusiveModeStream(streams, providerFormat))
        {
            // We do not need the resampler; HAL is working for us now.
            selectedStream = 0;
            requiresResampler = false;
            System.Console.WriteLine("We have assigned our own audio format to HAL.");
        }
        */

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
            uint perChannelBytes = asbdToUse.mBytesPerFrame / asbdToUse.mChannelsPerFrame;
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

        uint reportedLatency = selectedDevice.BufferFrameSize;

        // Configure the player source. If we need the resampler,
        // we inject the resampler and pass the provider through it.
        if (requiresResampler)
        {
            ChannelLayoutHandle handle = null;
            // Attempt to get from the HAL the expected channel layout.
            try
            {
                handle = selectedDevice.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output);
            }
            catch (CoreAudioPropertyNotFoundException)
            {
                // No preferred channel layout reported by the device
                handle = null;
            }
            try
            {
                // Configure the resampler as appropriate.
                selectedSource = new ResamplerSource(
                    originalProvider,
                    asbdToUse,
                    handle, // we can pass null here!
                    reportedLatency
                );
            }
            finally
            {
                handle?.Dispose();
            }
        }
        else
        {
            // Otherwise, just use our wave provider directly.
            selectedSource = new RawSource(originalProvider, asbdToUse, reportedLatency);
        }

        try
        {
            // Release any previously allocated I/O procedure, if so required.
            if (isInit) { ioProcedure?.Dispose(); }
            // Now, enable only the stream(s) we need.
            bool[] enabledStreams = new bool[streams.Length];
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved))
            {
                // Non-interleaved case
                if (isInit || ioProcedure is InterleavedProcedure)
                {
                    // Make sure that any state that is referenced
                    // to the procedure gets cleaned up.
                    if (ioProcedure is not null)
                    {
                        ioProcedure.PlaybackStopped -= FirePlaybackStopped;
                        ioProcedure.Dispose();
                        ioProcedure = null;
                    }
                    ioProcedure = new NonInterleavedProcedure(selectedDevice);
                    ioProcedure.PlaybackStopped += FirePlaybackStopped;
                }
                Array.Fill(enabledStreams, true);
            }
            else
            {
                // Interleaved case
                if (isInit || ioProcedure is NonInterleavedProcedure)
                {
                    // Make sure that any state that is referenced
                    // to the procedure gets cleaned up.
                    if (ioProcedure is not null)
                    {
                        ioProcedure.PlaybackStopped -= FirePlaybackStopped;
                        ioProcedure.Dispose();
                        ioProcedure = null;
                    }
                    ioProcedure = new InterleavedProcedure(selectedDevice);
                    ioProcedure.PlaybackStopped += FirePlaybackStopped;
                }
                for (int I = 0; I < streams.Length; I++)
                {
                    enabledStreams[I] = I == selectedStream;
                }
            }
            ioProcedure.Source = selectedSource;
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

    // Constructs appropriate stream descriptions for initializing
    // an audio device into the exclusive mode, where we can specify
    // any PCM format of our liking.
    // Probably this does not work, because the HAL does whatever it wants with
    // the streams, even if so setting to exclusive mode and then doing all of this 
    // does not assign the format we want to use.
    private bool TryConstructingExclusiveModeStream(AudioStream[] streams, WaveFormat providerFormat)
    {
        // Get valid bits per sample and IEEE float sample usage
        bool isIeeeFloat;
        uint validBitsPerSample;
        if (providerFormat is WaveFormatExtensible ex)
        {
            isIeeeFloat = ex.SubFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
            validBitsPerSample = (uint)ex.ValidBitsPerSample;
        }
        else
        {
            isIeeeFloat = providerFormat.Encoding == WaveFormatEncoding.IeeeFloat;
            validBitsPerSample = (uint)providerFormat.BitsPerSample;
        }
        if (HasStateFlagFast(CoreAudioPlayerStateFlags.NonInterleaved))
        {
            uint channelCount = 0U;
            AudioStreamBasicDescription tempAsbd, ccAsbd;
            // Get the current ASBD structures comprising the streams.
            var originalDescriptions = new AudioStreamBasicDescription[streams.Length];
            // Holds the ASBD structures we require.
            var requiredDescriptions = new AudioStreamBasicDescription[streams.Length];
            for (int I = 0; I < streams.Length; I++)
            {
                tempAsbd = originalDescriptions[I] = streams[I].VirtualFormatNative;
                channelCount += tempAsbd.mChannelsPerFrame;
                ccAsbd = new();
                ccAsbd.mFramesPerPacket = 1U;
                ccAsbd.mBitsPerChannel = validBitsPerSample;
                ccAsbd.mSampleRate = providerFormat.SampleRate;
                ccAsbd.mChannelsPerFrame = tempAsbd.mChannelsPerFrame;
                ccAsbd.mFormatID = AudioFormatIDs.kAudioFormatLinearPCM;
                ccAsbd.mFormatFlags = AudioFormatFlags.kAudioFormatFlagIsNonInterleaved;
                ccAsbd.mBytesPerPacket = ccAsbd.mBytesPerFrame = (uint)(providerFormat.BitsPerSample / 8);
                if (isIeeeFloat)
                {
                    ccAsbd.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsFloat;
                }
                else
                {
                    ccAsbd.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsSignedInteger;
                }
                if (!BitConverter.IsLittleEndian) { ccAsbd.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsBigEndian; }
                requiredDescriptions[I] = ccAsbd;
            }
            if (channelCount != providerFormat.Channels)
            {
                throw new InvalidOperationException("Attempted to initialize exclusive mode on a device not matching with the provider's channel count.");
            }
            else
            {
                // Apply all the virtual formats.
                for (int I = 0; I < streams.Length; I++)
                {
                    try
                    {
                        streams[I].VirtualFormatNative = requiredDescriptions[I];
                    }
                    catch (CoreAudioException e)
                    {
                        // If an assignment fails, we cannot define our own format;
                        // try with the resampler instead.
                        // System.Console.WriteLine("[CoreAudioPlayerInit/NON_INTERLEAVED]: Cannot construct exclusive mode player: " + e);
                        Debug.WriteLine("[CoreAudioPlayerInit]: Cannot construct exclusive mode player: " + e);
                        // Revert all the applied formats (revert even those that were not modified yet)
                        for (int J = 0; J < streams.Length; J++)
                        {
                            try
                            {
                                streams[J].VirtualFormatNative = originalDescriptions[J];
                            }
                            catch (CoreAudioException e2)
                            {
                                Debug.WriteLine($"[CoreAudioPlayerInit]: Cannot revert virtual format on stream index {J}: {e2}");
                            }
                        }
                        return false;
                    }
                }
            }
        }
        else
        {
            // Pick the first stream and modify it for our needs.
            var ctAsbd = new AudioStreamBasicDescription();
            // Get the current virtual format to apply if the virtual format assignment fails.
            var currentVf = streams[0].VirtualFormatNative;
            ctAsbd.mFramesPerPacket = 1U;
            ctAsbd.mBitsPerChannel = validBitsPerSample;
            ctAsbd.mSampleRate = providerFormat.SampleRate;
            ctAsbd.mFormatID = AudioFormatIDs.kAudioFormatLinearPCM;
            ctAsbd.mChannelsPerFrame = (uint)providerFormat.Channels;
            ctAsbd.mBytesPerPacket = ctAsbd.mBytesPerFrame = (uint)(providerFormat.BitsPerSample / 8) * ctAsbd.mChannelsPerFrame;
            if (providerFormat.Channels != currentVf.mChannelsPerFrame)
            {
                // System.Console.WriteLine("[CoreAudioPlayerInit]: # of Channels do not match");
                return false;
            }
            else if (isIeeeFloat)
            {
                ctAsbd.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsFloat;
            }
            else
            {
                ctAsbd.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsSignedInteger;
            }
            if (!BitConverter.IsLittleEndian) { ctAsbd.mFormatFlags |= AudioFormatFlags.kAudioFormatFlagIsBigEndian; }
            try
            {
                streams[0].VirtualFormatNative = ctAsbd;
            }
            catch (CoreAudioException e)
            {
                // System.Console.WriteLine("[CoreAudioPlayerInit/INTERLEAVED]: Cannot construct exclusive mode player: " + e);
                Debug.WriteLine("[CoreAudioPlayerInit]: Cannot construct exclusive mode player: " + e);
                // Make sure that the virtual format is reverted
                // (although that HAL does not modify the current 
                // format unless it succeeds setting the virtual
                // format)
                try
                {
                    streams[0].VirtualFormatNative = currentVf;
                }
                catch (CoreAudioException e2)
                {
                    Debug.WriteLine("[CoreAudioPlayerInit]: Cannot revert virtual format: " + e2);
                }
                return false;
            }
        }

        // System.Console.WriteLine("[CoreAudioPlayerInit]: Hog mode assignment succeeded");
        return true;
    }
}