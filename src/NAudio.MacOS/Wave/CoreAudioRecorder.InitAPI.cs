
using System;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

// Provides the management and initialization code of the recorder. 
// Anything that manages the recorder and the I/O procedure should be here.
public partial class CoreAudioRecorder
{
    private void OnDeviceWillBeDestroyed(AudioObject _)
    {
        flags |= CoreAudioRecorderStateFlags.Invalidated;

        // If we have an I/O procedure attached, do the following.
        if (ioProcedure is not null && ioProcedure.IsRunning)
        {
            // Now, dispatch the recording stopped event with the event args containing
            // a Core Audio exception saying the device is no longer available.
            OnRecodingStopped(new CoreAudioException(
                "The device used to perform I/O is now gone.",
                MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError
            ));
        }
    }

    private void OnVirtualFormatChanged(AudioObject stream)
    {
        // Stop recording if is running. 
        // The user will restart the recording if so he needs.
        if (flags.HasFlag(CoreAudioRecorderStateFlags.Initialized)) { StopRecording(); }
        Initialize();
        CaptureFormatChanged?.Invoke(this);
    }

    private void OnStreamsChanged(AudioObject _)
    {
        bool oldState = ioProcedure.IsRunning;
        ioProcedure.Stop();
        state = CaptureState.Stopped;
        Initialize();
        if (oldState) { ioProcedure.Start(); }
    }

    private void Initialize(bool isInit = false)
    {
        flags &= ~CoreAudioRecorderStateFlags.NonInterleaved;
        MacUtils.EnsureDisposableObjectsDisposed(virtualFormatChanged, streamsChanged);

        int selectedIndex = -1;
        var streams = selectedDevice.GetStreams(AudioObjectPropertyScopeConstants.Input);

        AudioStreamBasicDescription virtualFormat = default;
        foreach (var s in streams)
        {
            selectedIndex++;
            virtualFormat = s.VirtualFormatNative;
            if (virtualFormat.mFormatID != AudioFormatIDs.kAudioFormatLinearPCM)
            {
                // Not supported, see whether we have any other streams
                continue;
            }
            else if (
                virtualFormat.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved) ||
                (virtualFormat.mChannelsPerFrame == 1U && streams.Length > 1)
            )
            {
                // We have non-interleaved case.
                flags |= CoreAudioRecorderStateFlags.NonInterleaved;
                break;
            }
            else
            {
                // Use the current stream.
                break;
            }
        }

        if (selectedIndex == -1)
        {
            throw new InvalidOperationException("The specified device does not provide any streams to perform capture on!");
        }

        uint totalLatency = 0U;

        // Create the I/O procedure, assign the streams to use, then compute the latency.
        bool[] streamSelection = new bool[streams.Length];
        if (flags.HasFlag(CoreAudioRecorderStateFlags.NonInterleaved))
        {
            if (isInit || ioProcedure is InterleavedProcedure)
            {
                ioProcedure?.Dispose();
                ioProcedure = null;
                ioProcedure = new NonInterleavedProcedure(selectedDevice);
            }
            uint channelCount = 0U;
            foreach (var s in streams)
            {
                channelCount += s.VirtualFormatNative.mChannelsPerFrame;
                totalLatency += s.Latency;
            }
            virtualFormat.mChannelsPerFrame = channelCount;
            Array.Fill(streamSelection, true);
        }
        else
        {
            if (isInit || ioProcedure is NonInterleavedProcedure)
            {
                ioProcedure?.Dispose();
                ioProcedure = null;
                ioProcedure = new InterleavedProcedure(selectedDevice);
            }
            for (int I = 0; I < streams.Length; I++) { streamSelection[I] = I == selectedIndex; }
            totalLatency = streams[selectedIndex].Latency;
        }

        selectedDevice.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Input, streamSelection);
        ioProcedure.StreamLatency = totalLatency;
        ioProcedure.VirtualFormat = virtualFormat;
        // The latency of the streams may report as zero - 
        // in such case, use the buffer frame size divided by the sample rate.
        if (totalLatency == 0U)
        {
            ioProcedure.StreamLatency = (uint)(selectedDevice.BufferFrameSize / virtualFormat.mSampleRate);
        }
        virtualFormatChanged = streams[selectedIndex].ConstructVirtualFormatChangedEvent();
        virtualFormatChanged.Event += OnVirtualFormatChanged;
        try
        {
            OnInitializationComplete();
        }
        catch
        {
            MacUtils.EnsureDisposableObjectsDisposed(ioProcedure, virtualFormatChanged);
            throw;
        }

    }

}
