using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Wave.DirectSoundInterop;

namespace NAudio.Wave
{
    /// <summary>
    /// NativeDirectSoundOut using DirectSound COM interop.
    /// Contact author: Alexandre Mutel - alexandre_mutel at yahoo.fr
    /// Modified by: Graham "Gee" Plumb
    /// </summary>
    public partial class DirectSoundOut : IWavePlayer
    {
        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        private PlaybackState playbackState;
        private WaveFormat waveFormat;
        private int samplesTotalSize;
        private int samplesFrameSize;
        private int nextSamplesWriteIndex;
        private int desiredLatency;
        private Guid device;
        private byte[] samples;
        private IWaveProvider waveStream = null;
        private IDirectSound directSound = null;
        private IDirectSoundBuffer primarySoundBuffer = null;
        private IDirectSoundBuffer secondaryBuffer = null;
        private EventWaitHandle frameEventWaitHandle1;
        private EventWaitHandle frameEventWaitHandle2;
        private EventWaitHandle endEventWaitHandle;
        private Thread notifyThread;
        private SynchronizationContext syncContext;
        private long bytesPlayed;

        // Used purely for locking
        private Object m_LockObject = new Object();

        /// <summary>
        /// Gets the DirectSound output devices in the system
        /// </summary>
        public static unsafe IEnumerable<DirectSoundDeviceInfo> Devices
        {
            get
            {
                devices = new List<DirectSoundDeviceInfo>();
                delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, int> thunk = &EnumCallbackThunk;
                DirectSoundException.ThrowIfFailed(
                    DirectSoundEnumerate((IntPtr)thunk, IntPtr.Zero));
                return devices;
            }
        }

        private static List<DirectSoundDeviceInfo> devices;

        // BOOL-returning [UnmanagedCallersOnly] thunk invoked from native by
        // DirectSoundEnumerate. Replaces the old delegate-based callback so the
        // dispatch is zero-allocation and fully AOT-clean — Marshal.GetFunctionPointerForDelegate
        // is supported under AOT but a static unmanaged callback avoids the delegate
        // pinning + indirection entirely.
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private static int EnumCallbackThunk(IntPtr lpGuid, IntPtr lpcstrDescription, IntPtr lpcstrModule, IntPtr lpContext)
        {
            var device = new DirectSoundDeviceInfo();
            if (lpGuid == IntPtr.Zero)
            {
                device.Guid = Guid.Empty;
            }
            else
            {
                byte[] guidBytes = new byte[16];
                Marshal.Copy(lpGuid, guidBytes, 0, 16);
                device.Guid = new Guid(guidBytes);
            }
            device.Description = Marshal.PtrToStringAnsi(lpcstrDescription);
            if (lpcstrModule != IntPtr.Zero)
            {
                device.ModuleName = Marshal.PtrToStringAnsi(lpcstrModule);
            }
            devices.Add(device);
            return 1; // BOOL TRUE — continue enumeration
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DirectSoundOut"/> class.
        /// </summary>
        public DirectSoundOut()
            : this(DSDEVID_DefaultPlayback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectSoundOut"/> class.
        /// </summary>
        public DirectSoundOut(Guid device)
            : this(device, 40)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectSoundOut"/> class.
        /// </summary>
        public DirectSoundOut(int latency)
            : this(DSDEVID_DefaultPlayback, latency)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectSoundOut"/> class.
        /// (40ms seems to work under Vista).
        /// </summary>
        /// <param name="latency">The latency.</param>
        /// <param name="device">Selected device</param>
        public DirectSoundOut(Guid device, int latency)
        {
            if (device == Guid.Empty)
            {
                device = DSDEVID_DefaultPlayback;
            }
            this.device = device;
            this.desiredLatency = latency;
            this.syncContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="DirectSoundOut"/> is reclaimed by garbage collection.
        /// </summary>
        ~DirectSoundOut()
        {
            Dispose();
        }

        /// <summary>
        /// Begin playback
        /// </summary>
        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                // -------------------------------------------------------------------------------------
                // Thread that process samples
                // -------------------------------------------------------------------------------------
                notifyThread = new Thread(new ThreadStart(PlaybackThreadFunc));
                // put this back to highest when we are confident we don't have any bugs in the thread proc
                notifyThread.Priority = ThreadPriority.Normal;
                notifyThread.IsBackground = true;
                notifyThread.Start();
            }

            lock (m_LockObject)
            {
                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        public void Stop()
        {
            // Try and tidy up nicely
            if (Monitor.TryEnter(m_LockObject, 50))
            {
                playbackState = PlaybackState.Stopped;
                Monitor.Exit(m_LockObject);
            }
            else
            {
                // No joy - abort the thread!
                if (notifyThread != null)
                {
                    notifyThread = null;
                    // Thread.Abort is not supported on .NET 8+; the thread will
                    // exit on its own when it next checks playbackState.
                }
            }
        }

        /// <summary>
        /// Pause Playback
        /// </summary>
        public void Pause()
        {
            lock (m_LockObject)
            {
                playbackState = PlaybackState.Paused;
            }
        }

        /// <summary>
        /// Gets the current position in bytes from the wave output device.
        /// (n.b. this is not the same thing as the position within your reader
        /// stream)
        /// </summary>
        /// <returns>Position in bytes</returns>
        public long GetPosition()
        {
            if (playbackState != Wave.PlaybackState.Stopped)
            {
                var sbuf = secondaryBuffer;
                if (sbuf != null)
                {
                    DirectSoundException.ThrowIfFailed(sbuf.GetCurrentPosition(out uint currentPlayCursor, out _));
                    return currentPlayCursor + bytesPlayed;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the current position from the wave output device.
        /// </summary>
        public TimeSpan PlaybackPosition
        {
            get
            {
                // bytes played in this stream
                var pos = GetPosition();

                // samples played in this stream
                pos /= waveFormat.Channels * waveFormat.BitsPerSample / 8;

                // ms played in this stream
                return TimeSpan.FromMilliseconds(pos * 1000.0 / waveFormat.SampleRate);
            }
        }


        /// <summary>
        /// Initialise playback
        /// </summary>
        /// <param name="waveProvider">The wave provider to be played</param>
        public void Init(IWaveProvider waveProvider)
        {
            this.waveStream = waveProvider;
            this.waveFormat = waveProvider.WaveFormat;
        }

        private void InitializeDirectSound()
        {
            // Open DirectSound
            lock (this.m_LockObject)
            {
                Debug.Assert(directSound == null);

                // Activate IDirectSound via the source-generated wrapper instead of
                // [Out, MarshalAs(UnmanagedType.Interface)] which depends on the
                // built-in COM interop machinery the trimmer strips (issue #1191).
                DirectSoundException.ThrowIfFailed(DirectSoundCreate(in device, out IntPtr directSoundPtr, IntPtr.Zero));
                try
                {
                    directSound = (IDirectSound)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        directSoundPtr, CreateObjectFlags.UniqueInstance);
                }
                finally
                {
                    Marshal.Release(directSoundPtr);
                }

                // Set Cooperative Level to PRIORITY (priority level can call the SetFormat and Compact methods)
                DirectSoundException.ThrowIfFailed(directSound.SetCooperativeLevel(GetDesktopWindow(), DirectSoundCooperativeLevel.DSSCL_PRIORITY));

                // -------------------------------------------------------------------------------------
                // Create PrimaryBuffer
                // -------------------------------------------------------------------------------------

                // Fill BufferDescription for PrimaryBuffer
                var bufferDesc = new BufferDescription
                {
                    dwSize = Marshal.SizeOf<BufferDescription>(),
                    dwBufferBytes = 0,
                    dwFlags = DirectSoundBufferCaps.DSBCAPS_PRIMARYBUFFER,
                    dwReserved = 0,
                    lpwfxFormat = IntPtr.Zero,
                    guidAlgo = Guid.Empty,
                };

                // Create PrimaryBuffer
                DirectSoundException.ThrowIfFailed(directSound.CreateSoundBuffer(in bufferDesc, out IntPtr primaryBufferPtr, IntPtr.Zero));
                try
                {
                    primarySoundBuffer = (IDirectSoundBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        primaryBufferPtr, CreateObjectFlags.UniqueInstance);
                }
                finally
                {
                    Marshal.Release(primaryBufferPtr);
                }

                // Play & Loop on the PrimarySound Buffer
                DirectSoundException.ThrowIfFailed(primarySoundBuffer.Play(0, 0, DirectSoundPlayFlags.DSBPLAY_LOOPING));

                // -------------------------------------------------------------------------------------
                // Create SecondaryBuffer
                // -------------------------------------------------------------------------------------

                // A frame of samples equals to Desired Latency
                samplesFrameSize = MsToBytes(desiredLatency);

                // Fill BufferDescription for SecondaryBuffer
                GCHandle handleOnWaveFormat = GCHandle.Alloc(waveFormat, GCHandleType.Pinned); // Ptr to waveFormat
                IntPtr secondaryBufferPtr;
                try
                {
                    var bufferDesc2 = new BufferDescription
                    {
                        dwSize = Marshal.SizeOf<BufferDescription>(),
                        dwBufferBytes = (uint)(samplesFrameSize * 2),
                        dwFlags = DirectSoundBufferCaps.DSBCAPS_GETCURRENTPOSITION2
                            | DirectSoundBufferCaps.DSBCAPS_CTRLPOSITIONNOTIFY
                            | DirectSoundBufferCaps.DSBCAPS_GLOBALFOCUS
                            | DirectSoundBufferCaps.DSBCAPS_CTRLVOLUME
                            | DirectSoundBufferCaps.DSBCAPS_STICKYFOCUS
                            | DirectSoundBufferCaps.DSBCAPS_GETCURRENTPOSITION2,
                        dwReserved = 0,
                        lpwfxFormat = handleOnWaveFormat.AddrOfPinnedObject(), // Ptr to waveFormat
                        guidAlgo = Guid.Empty,
                    };

                    // Create SecondaryBuffer
                    DirectSoundException.ThrowIfFailed(directSound.CreateSoundBuffer(in bufferDesc2, out secondaryBufferPtr, IntPtr.Zero));
                }
                finally
                {
                    handleOnWaveFormat.Free();
                }
                try
                {
                    secondaryBuffer = (IDirectSoundBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                        secondaryBufferPtr, CreateObjectFlags.UniqueInstance);

                    // Get effective SecondaryBuffer size
                    var dsbCaps = new BufferCaps { dwSize = Marshal.SizeOf<BufferCaps>() };
                    DirectSoundException.ThrowIfFailed(secondaryBuffer.GetCaps(ref dsbCaps));

                    nextSamplesWriteIndex = 0;
                    samplesTotalSize = dsbCaps.dwBufferBytes;
                    samples = new byte[samplesTotalSize];
                    System.Diagnostics.Debug.Assert(samplesTotalSize == (2 * samplesFrameSize), "Invalid SamplesTotalSize vs SamplesFrameSize");

                    // -------------------------------------------------------------------------------------
                    // Create double buffering notification.
                    // Use DirectSoundNotify at Position [0, 1/2] and Stop Position (0xFFFFFFFF)
                    // -------------------------------------------------------------------------------------

                    // QI from the secondary-buffer raw pointer for IDirectSoundNotify. Source-generated
                    // RCWs do not auto-QI on a sibling-interface cast — we must walk the underlying
                    // IUnknown pointer ourselves.
                    Guid iidNotify = typeof(IDirectSoundNotify).GUID;
                    int hr = Marshal.QueryInterface(secondaryBufferPtr, in iidNotify, out IntPtr notifyPtr);
                    DirectSoundException.ThrowIfFailed(hr);

                    IDirectSoundNotify notify = null;
                    try
                    {
                        notify = (IDirectSoundNotify)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                            notifyPtr, CreateObjectFlags.UniqueInstance);

                        frameEventWaitHandle1 = new EventWaitHandle(false, EventResetMode.AutoReset);
                        frameEventWaitHandle2 = new EventWaitHandle(false, EventResetMode.AutoReset);
                        endEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

                        var notifies = new DirectSoundBufferPositionNotify[3];
                        notifies[0].dwOffset = 0;
                        notifies[0].hEventNotify = frameEventWaitHandle1.SafeWaitHandle.DangerousGetHandle();
                        notifies[1].dwOffset = (uint)samplesFrameSize;
                        notifies[1].hEventNotify = frameEventWaitHandle2.SafeWaitHandle.DangerousGetHandle();
                        notifies[2].dwOffset = 0xFFFFFFFF;
                        notifies[2].hEventNotify = endEventWaitHandle.SafeWaitHandle.DangerousGetHandle();

                        GCHandle pinNotifies = GCHandle.Alloc(notifies, GCHandleType.Pinned);
                        try
                        {
                            DirectSoundException.ThrowIfFailed(notify.SetNotificationPositions(3, pinNotifies.AddrOfPinnedObject()));
                        }
                        finally
                        {
                            pinNotifies.Free();
                        }
                    }
                    finally
                    {
                        // Notify wrapper is single-use — release on every path so that a
                        // SetNotificationPositions failure (or a projection failure that
                        // left notify null) cannot leak resources. ComObject's finalizer
                        // would eventually run for a leaked wrapper, but deterministic
                        // release is the contract here.
                        if (notify != null)
                        {
                            ((ComObject)(object)notify).FinalRelease();
                        }
                        Marshal.Release(notifyPtr);
                    }
                }
                finally
                {
                    // Wrapper has its own AddRef'd ref — drop the activation ref we still hold.
                    Marshal.Release(secondaryBufferPtr);
                }
            }
        }

        /// <summary>
        /// Current playback state
        /// </summary>
        /// <value></value>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }

        /// <summary>
        /// The volume 1.0 is full scale
        /// </summary>
        /// <value></value>
        public float Volume
        {
            get
            {
                return 1.0f;
            }
            set
            {
                if (value != 1.0f)
                {
                    throw new InvalidOperationException("Setting volume not supported on DirectSoundOut, adjust the volume on your WaveProvider instead");
                }
            }
        }

        /// <inheritdoc/>
        public WaveFormat OutputWaveFormat => waveFormat;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Determines whether the SecondaryBuffer is lost.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if [is buffer lost]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsBufferLost()
        {
            DirectSoundException.ThrowIfFailed(secondaryBuffer.GetStatus(out DirectSoundBufferStatus status));
            return (status & DirectSoundBufferStatus.DSBSTATUS_BUFFERLOST) != 0;
        }

        /// <summary>
        /// Convert ms to bytes size according to WaveFormat
        /// </summary>
        /// <param name="ms">The ms</param>
        /// <returns>number of byttes</returns>
        private int MsToBytes(int ms)
        {
            int bytes = ms * (waveFormat.AverageBytesPerSecond / 1000);
            bytes -= bytes % waveFormat.BlockAlign;
            return bytes;
        }

        /// <summary>
        /// Processes the samples in a separate thread.
        /// </summary>
        private void PlaybackThreadFunc()
        {
            // Used to determine if playback is halted
            bool lPlaybackHalted = false;
            bool firstBufferStarted = false;
            bytesPlayed = 0;

            Exception exception = null;
            // Incase the thread is killed
            try
            {
                InitializeDirectSound();
                int lResult = 1;

                if (PlaybackState == PlaybackState.Stopped)
                {
                    DirectSoundException.ThrowIfFailed(secondaryBuffer.SetCurrentPosition(0));
                    nextSamplesWriteIndex = 0;
                    lResult = Feed(samplesTotalSize);
                }

                // Incase the previous Feed method returns 0
                if (lResult > 0)
                {
                    lock (m_LockObject)
                    {
                        playbackState = PlaybackState.Playing;
                    }

                    DirectSoundException.ThrowIfFailed(secondaryBuffer.Play(0, 0, DirectSoundPlayFlags.DSBPLAY_LOOPING));

                    var waitHandles = new WaitHandle[] { frameEventWaitHandle1, frameEventWaitHandle2, endEventWaitHandle };

                    bool lContinuePlayback = true;
                    while (PlaybackState != PlaybackState.Stopped && lContinuePlayback)
                    {
                        // Wait for signals on frameEventWaitHandle1 (Position 0), frameEventWaitHandle2 (Position 1/2)
                        int indexHandle = WaitHandle.WaitAny(waitHandles, 3 * desiredLatency, false);

                        // TimeOut is ok
                        if (indexHandle != WaitHandle.WaitTimeout)
                        {
                            // Buffer is Stopped
                            if (indexHandle == 2)
                            {
                                // (Gee) - Not sure whether to stop playback in this case or not!
                                StopPlayback();
                                lPlaybackHalted = true;
                                lContinuePlayback = false;
                            }
                            else
                            {
                                if (indexHandle == 0)
                                {
                                    // we're at the beginning of the buffer...
                                    if (firstBufferStarted)
                                    {
                                        // because this notification is based on the *playback" cursor, this should be reasonably accurate
                                        bytesPlayed += samplesFrameSize * 2;
                                    }
                                }
                                else
                                {
                                    firstBufferStarted = true;
                                }

                                indexHandle = (indexHandle == 0) ? 1 : 0;
                                nextSamplesWriteIndex = indexHandle * samplesFrameSize;

                                // Only carry on playing if we can!
                                if (Feed(samplesFrameSize) == 0)
                                {
                                    StopPlayback();
                                    lPlaybackHalted = true;
                                    lContinuePlayback = false;
                                }
                            }
                        }
                        else
                        {
                            // Timed out!
                            StopPlayback();
                            lPlaybackHalted = true;
                            lContinuePlayback = false;
                            // report this as an error in the Playback Stopped
                            // seems to happen when device is unplugged
                            throw new InvalidOperationException("DirectSound buffer timeout");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Do nothing (except report error)
                Debug.WriteLine(e.ToString());
                exception = e;
            }
            finally
            {
                if (!lPlaybackHalted)
                {
                    try
                    {
                        StopPlayback();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        // don't overwrite the original reason we exited the playback loop
                        if (exception == null) exception = e;
                    }
                }

                lock (m_LockObject)
                {
                    playbackState = PlaybackState.Stopped;
                }

                bytesPlayed = 0;

                // Fire playback stopped event
                RaisePlaybackStopped(exception);
            }
        }

        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (this.syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }


        /// <summary>
        /// Stop playback
        /// </summary>
        private void StopPlayback()
        {
            lock (this.m_LockObject)
            {
                if (secondaryBuffer != null)
                {
                    CleanUpSecondaryBuffer();

                    secondaryBuffer.Stop();
                    ((ComObject)(object)secondaryBuffer).FinalRelease();
                    secondaryBuffer = null;
                }
                if (primarySoundBuffer != null)
                {
                    primarySoundBuffer.Stop();
                    ((ComObject)(object)primarySoundBuffer).FinalRelease();
                    primarySoundBuffer = null;
                }
                if (directSound != null)
                {
                    ((ComObject)(object)directSound).FinalRelease();
                    directSound = null;
                }
            }
        }

        /// <summary>
        /// Clean up the SecondaryBuffer
        /// </summary>
        /// <remarks>
        /// <para>
        /// In DirectSound, when playback is started,
        /// the rest of the sound that was played last time is played back as noise.
        /// This happens even if the secondary buffer is completely silenced,
        /// so it seems that the buffer in the primary buffer or higher is not cleared.
        /// </para>
        /// <para>
        /// To solve this problem fill the secondary buffer with silence data when stop playback.
        /// </para>
        /// </remarks>
        private void CleanUpSecondaryBuffer()
        {
            if (secondaryBuffer != null)
            {
                byte[] silence = new byte[samplesTotalSize];

                // Lock the SecondaryBuffer
                DirectSoundException.ThrowIfFailed(secondaryBuffer.Lock(0, (uint)samplesTotalSize,
                    out IntPtr wavBuffer1, out int nbSamples1,
                    out IntPtr wavBuffer2, out int nbSamples2,
                    DirectSoundBufferLockFlag.None));

                // Copy silence data to the SecondaryBuffer
                if (wavBuffer1 != IntPtr.Zero)
                {
                    Marshal.Copy(silence, 0, wavBuffer1, nbSamples1);
                    if (wavBuffer2 != IntPtr.Zero)
                    {
                        Marshal.Copy(silence, nbSamples1, wavBuffer2, nbSamples2);
                    }
                }

                // Unlock the SecondaryBuffer
                DirectSoundException.ThrowIfFailed(secondaryBuffer.Unlock(wavBuffer1, nbSamples1, wavBuffer2, nbSamples2));
            }
        }


        /// <summary>
        /// Feeds the SecondaryBuffer with the WaveStream
        /// </summary>
        /// <param name="bytesToCopy">number of bytes to feed</param>
        private int Feed(int bytesToCopy)
        {
            int bytesRead = bytesToCopy;

            // Restore the buffer if lost
            if (IsBufferLost())
            {
                DirectSoundException.ThrowIfFailed(secondaryBuffer.Restore());
            }

            // Clear the bufferSamples if in Paused
            if (playbackState == PlaybackState.Paused)
            {
                Array.Clear(samples, 0, samples.Length);
            }
            else
            {
                // Read data from stream (Should this be inserted between the lock / unlock?)
                bytesRead = waveStream.Read(samples.AsSpan(0, bytesToCopy));

                if (bytesRead == 0)
                {
                    Array.Clear(samples, 0, samples.Length);
                    return 0;
                }
            }

            // Lock a portion of the SecondaryBuffer (starting from 0 or 1/2 the buffer)
            DirectSoundException.ThrowIfFailed(secondaryBuffer.Lock(nextSamplesWriteIndex, (uint)bytesRead,
                out IntPtr wavBuffer1, out int nbSamples1,
                out IntPtr wavBuffer2, out int nbSamples2,
                DirectSoundBufferLockFlag.None));

            // Copy back to the SecondaryBuffer
            if (wavBuffer1 != IntPtr.Zero)
            {
                Marshal.Copy(samples, 0, wavBuffer1, nbSamples1);
                if (wavBuffer2 != IntPtr.Zero)
                {
                    Marshal.Copy(samples, nbSamples1, wavBuffer2, nbSamples2);
                }
            }

            // Unlock the SecondaryBuffer
            DirectSoundException.ThrowIfFailed(secondaryBuffer.Unlock(wavBuffer1, nbSamples1, wavBuffer2, nbSamples2));

            return bytesRead;
        }


        //----------------------------------------------------------------------------------------------
        // Native DirectSound entry points
        //----------------------------------------------------------------------------------------------

        /// <summary>
        /// Instanciate DirectSound from the DLL. Returns the raw IUnknown pointer; caller must
        /// project via <see cref="ComActivation.ComWrappers"/> and release the original ref.
        /// </summary>
        [LibraryImport("dsound.dll", EntryPoint = "DirectSoundCreate")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private static partial int DirectSoundCreate(in Guid GUID, out IntPtr directSound, IntPtr pUnkOuter);


        /// <summary>
        /// DirectSound default playback device GUID
        /// </summary>
        public static readonly Guid DSDEVID_DefaultPlayback = new Guid("DEF00000-9C6D-47ED-AAF1-4DDA8F2B5C03");

        /// <summary>
        /// DirectSound default capture device GUID
        /// </summary>
        public static readonly Guid DSDEVID_DefaultCapture = new Guid("DEF00001-9C6D-47ED-AAF1-4DDA8F2B5C03");

        /// <summary>
        /// DirectSound default device for voice playback
        /// </summary>
        public static readonly Guid DSDEVID_DefaultVoicePlayback = new Guid("DEF00002-9C6D-47ED-AAF1-4DDA8F2B5C03");

        /// <summary>
        /// DirectSound default device for voice capture
        /// </summary>
        public static readonly Guid DSDEVID_DefaultVoiceCapture = new Guid("DEF00003-9C6D-47ED-AAF1-4DDA8F2B5C03");

        /// <summary>
        /// The DirectSoundEnumerate function enumerates the DirectSound drivers installed in the system.
        /// The first argument is a pointer to a <c>DSEnumCallback</c> function — supplied here as
        /// an <see cref="UnmanagedCallersOnlyAttribute"/> static thunk via C# function-pointer syntax.
        /// </summary>
        /// <param name="lpDSEnumCallback">function pointer to callback</param>
        /// <param name="lpContext">User context</param>
        [LibraryImport("dsound.dll", EntryPoint = "DirectSoundEnumerateA")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        private static partial int DirectSoundEnumerate(IntPtr lpDSEnumCallback, IntPtr lpContext);

        /// <summary>
        /// Gets the HANDLE of the desktop window.
        /// </summary>
        /// <returns>HANDLE of the Desktop window</returns>
        [LibraryImport("user32.dll")]
        private static partial IntPtr GetDesktopWindow();
    }

    /// <summary>
    /// Class for enumerating DirectSound devices
    /// </summary>
    public class DirectSoundDeviceInfo
    {
        /// <summary>
        /// The device identifier
        /// </summary>
        public Guid Guid { get; set; }
        /// <summary>
        /// Device description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Device module name
        /// </summary>
        public string ModuleName { get; set; }
    }

}
