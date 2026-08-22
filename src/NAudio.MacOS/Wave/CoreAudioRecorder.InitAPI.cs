
using System;
using System.Threading;

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
        if (flags.HasFlag(CoreAudioRecorderStateFlags.Initialized))
        {
            // We take the lock to ensure that we can safely call
            // the StopRecordingInternal method - and we also
            // execute it only when not in stopped condition.
            Monitor.Enter(lockObject);
            try
            {
                if (state != CaptureState.Stopped)
                {
                    StopRecordingInternal();
                }
            }
            finally
            {
                Monitor.Exit(lockObject);
            }
        }
        Initialize();
        CaptureFormatChanged?.Invoke(this);
    }

    private void OnStreamsChanged(AudioObject _)
    {
        bool oldState = ioProcedure.IsRunning;
        // We take the lock to ensure that we can safely call
        // the StopRecordingInternal method - and we also
        // execute it only when not in stopped condition.
        Monitor.Enter(lockObject);
        try
        {
            if (state != CaptureState.Stopped)
            {
                StopRecordingInternal();
            }
        }
        finally
        {
            Monitor.Exit(lockObject);
        }
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
                var streamAsbd = s.VirtualFormatNative;
                channelCount += streamAsbd.mChannelsPerFrame;
                if (
                    virtualFormat.mSampleRate != streamAsbd.mSampleRate ||
                    virtualFormat.mBitsPerChannel != streamAsbd.mBitsPerChannel ||
                    virtualFormat.mFormatFlags != streamAsbd.mFormatFlags ||
                    virtualFormat.mChannelsPerFrame != streamAsbd.mChannelsPerFrame ||
                    virtualFormat.mBytesPerFrame != streamAsbd.mBytesPerFrame
                )
                {
                    throw new InvalidOperationException("Non-interleaved audio device with different audio formats is not supported");
                }
            }
            // There might be cases that drivers do not report the non-interleaved flag
            // even if they expect the client to provide the data as non-interleaved.
            // To fix this, the below if statement is required.
            if (!virtualFormat.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved))
            {
                // mBytesPerFrame will report the container size
                // of one channel in bytes, so we just multiply
                // by the number of channels.
                virtualFormat.mBytesPerFrame *= channelCount;
                virtualFormat.mBytesPerPacket = virtualFormat.mBytesPerFrame;
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
        }

        selectedDevice.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Input, streamSelection);
        ioProcedure.StreamLatency = selectedDevice.BufferFrameSize;
        ioProcedure.VirtualFormat = virtualFormat;
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
