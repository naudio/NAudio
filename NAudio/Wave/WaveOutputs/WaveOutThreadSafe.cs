using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;

namespace NAudio.Wave 
{
    // BIG TODO: how to report errors back from functions?

    /// <summary>
    /// Represents a wave out device
    /// </summary>
    [Obsolete("this was an experimental class, never properly completed")]
    public class WaveOutThreadSafe : IWavePlayer
    {
        private IntPtr hWaveOut;
        private WaveOutBuffer[] buffers;
        private IWaveProvider waveProvider;
        private int numBuffers;
        private PlaybackState playbackState;
        private WaveInterop.WaveCallback callback;
        private int devNumber;
        private int desiredLatency;
        private float volume = 1;
        private bool buffersQueued;
        private Thread waveOutThread;
        private Queue<WaveOutAction> actionQueue;
        private AutoResetEvent workAvailable;
        
        /// <summary>
        /// Playback stopped
        /// </summary>
        public event EventHandler PlaybackStopped;

        /// <summary>
        /// Retrieves the capabilities of a waveOut device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveOut device capabilities</returns>
        public static WaveOutCapabilities GetCapabilities(int devNumber)
        {
            WaveOutCapabilities caps = new WaveOutCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Returns the number of Wave Out devices available in the system
        /// </summary>
        public static Int32 DeviceCount
        {
            get
            {
                return WaveInterop.waveOutGetNumDevs();
            }
        }

        /// <summary>
        /// Opens a WaveOut device
        /// </summary>
        /// <param name="devNumber">This is the device number to open. 
        /// This must be between 0 and <see>DeviceCount</see> - 1.</param>
        /// <param name="desiredLatency">The number of milliseconds of audio to read before 
        /// streaming to the audio device. This will be broken into 3 buffers</param>
        public WaveOutThreadSafe(int devNumber, int desiredLatency)
        {
            this.devNumber = devNumber;
            this.desiredLatency = desiredLatency;
            this.callback = new WaveInterop.WaveCallback(Callback);
            actionQueue = new Queue<WaveOutAction>();
            workAvailable = new AutoResetEvent(false);
            waveOutThread = new Thread(new ThreadStart(ThreadProc));
            waveOutThread.Start();
        }

        private void ThreadProc()
        {
            while(true)
            {
                workAvailable.WaitOne();
                bool loop = true;
                while(loop)
                {
                    WaveOutAction waveOutAction = null;
                    lock(actionQueue)
                    {
                        if (actionQueue.Count > 0)
                        {
                            waveOutAction = actionQueue.Dequeue();
                        }
                    }
                    if(waveOutAction != null)
                    {
                        try
                        {

                            switch (waveOutAction.Function)
                            {
                                case WaveOutFunction.Init:
                                    Init((IWaveProvider)waveOutAction.Data);
                                    break;
                                case WaveOutFunction.Play:
                                    Play();
                                    break;
                                case WaveOutFunction.Pause:
                                    Pause();
                                    break;
                                case WaveOutFunction.Resume:
                                    Resume();
                                    break;
                                case WaveOutFunction.Stop:
                                    Stop();
                                    break;
                                case WaveOutFunction.BufferDone:
                                    OnBufferDone((WaveOutBuffer)waveOutAction.Data);
                                    break;
                                case WaveOutFunction.Exit:
                                    Exit();
                                    return;
                                case WaveOutFunction.SetVolume:
                                    SetVolume((int)waveOutAction.Data);
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            string s = e.ToString();
                            if (waveOutAction.Function == WaveOutFunction.Exit)
                            {
                                return;
                            }
                        }
                 
                    }
                    else
                    {
                        loop = false;
                    }
                }
            }
        }

        /// <summary>
        /// Initialises the WaveOut device
        /// </summary>
        /// <param name="waveProvider">Wave provider to play</param>
        public void Init(IWaveProvider waveProvider)
        {
            if(Thread.CurrentThread.ManagedThreadId != waveOutThread.ManagedThreadId)
            {
                lock(actionQueue)
                {
                    actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.Init,waveProvider));
                    workAvailable.Set();
                }
                return;
            }

            this.waveProvider = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize(desiredLatency);
            this.numBuffers = 3;

            MmException.Try(WaveInterop.waveOutOpen(out hWaveOut, (IntPtr)devNumber, waveProvider.WaveFormat, callback, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackFunction), "waveOutOpen");

            buffers = new WaveOutBuffer[numBuffers];
            playbackState = PlaybackState.Stopped;
            object waveOutLock = new object();
            for (int n = 0; n < numBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveProvider, waveOutLock);
            }
        }

        /// <summary>
        /// Start playing the audio from the WaveProvider
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                playbackState = PlaybackState.Playing;
            
                if(Thread.CurrentThread.ManagedThreadId != waveOutThread.ManagedThreadId)
                {
                    lock(actionQueue)
                    {
                        actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.Play,null));
                        workAvailable.Set();
                    }
                    return;
                }
            }

            if (!buffersQueued)
            {
                Pause(); // to avoid a deadlock - we don't want two waveOutWrites at once
                for (int n = 0; n < numBuffers; n++)
                {
                    System.Diagnostics.Debug.Assert(buffers[n].InQueue == false, "Adding a buffer that was already queued on play");
                    buffers[n].OnDone();
                }
                buffersQueued = true;
            }
            Resume();
        }

        /// <summary>
        /// Pause the audio
        /// </summary>
        public void Pause()
        {
            if(Thread.CurrentThread.ManagedThreadId != waveOutThread.ManagedThreadId)
            {
                lock(actionQueue)
                {
                    actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.Pause,null));
                    workAvailable.Set();
                }
                return;
            }

            MmResult result = WaveInterop.waveOutPause(hWaveOut);
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutPause");
            playbackState = PlaybackState.Paused;
        }

        /// <summary>
        /// Resume playing after a pause from the same position
        /// </summary>
        public void Resume()
        {
            if(Thread.CurrentThread.ManagedThreadId != waveOutThread.ManagedThreadId)
            {
                lock(actionQueue)
                {
                    actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.Resume,null));
                    workAvailable.Set();
                }
                return;
            }

            MmResult result = WaveInterop.waveOutRestart(hWaveOut);
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutRestart");
            playbackState = PlaybackState.Playing;
        }

        private void Exit()
        {
            if (hWaveOut != IntPtr.Zero)
            {
                Stop();
                WaveInterop.waveOutClose(hWaveOut);
                hWaveOut = IntPtr.Zero;
            }
            
            if (buffers != null)
            {
                for (int n = 0; n < numBuffers; n++)
                {
                    buffers[n].Dispose();
                }
                buffers = null;
            }
        }

        /// <summary>
        /// Stop and reset the WaveOut device
        /// </summary>
        public void Stop()
        {
            if(Thread.CurrentThread.ManagedThreadId != waveOutThread.ManagedThreadId)
            {
                lock(actionQueue)
                {
                    actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.Stop,null));
                    workAvailable.Set();
                }
                return;
            }

            playbackState = PlaybackState.Stopped;
            buffersQueued = false;
            MmResult result = WaveInterop.waveOutReset(hWaveOut);
            if (result != MmResult.NoError)
                throw new MmException(result, "waveOutReset");
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }


        /// <summary>
        /// Volume for this device 1.0 is full scale
        /// </summary>
        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
                float left = volume;
                float right = volume;
                int stereoVolume = (int)(left * 0xFFFF) + ((int)(right * 0xFFFF) << 16);
                lock (actionQueue)
                {
                    actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.SetVolume, stereoVolume));
                    workAvailable.Set();
                }                
            }
        }

        private void SetVolume(int stereoVolume)
        {
            MmException.Try(WaveInterop.waveOutSetVolume(hWaveOut, stereoVolume), "waveOutSetVolume");
        }

        #region Dispose Pattern

        /// <summary>
        /// Closes this WaveOut device
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Closes the WaveOut device and disposes of buffers
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected void Dispose(bool disposing)
        {

            lock (actionQueue)
            {
                actionQueue.Clear();
                actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.Exit,null));
                workAvailable.Set();
            }
            waveOutThread.Join();
        }

        /// <summary>
        /// Finalizer. Only called when user forgets to call <see>Dispose</see>
        /// </summary>
        ~WaveOutThreadSafe()
        {
            System.Diagnostics.Debug.Assert(false, "WaveOut device was not closed");
            Dispose(false);
        }

        #endregion

        // made non-static so that playing can be stopped here
        private void Callback(IntPtr hWaveOut, WaveInterop.WaveMessage uMsg, IntPtr dwUser, WaveHeader wavhdr, IntPtr dwReserved)
        {
            if (uMsg == WaveInterop.WaveMessage.WaveOutDone)
            {
                // check that we're not here through pressing stop
                GCHandle hBuffer = (GCHandle)wavhdr.userData;
                WaveOutBuffer buffer = (WaveOutBuffer)hBuffer.Target;

                lock(actionQueue)
                {
                    actionQueue.Enqueue(new WaveOutAction(WaveOutFunction.BufferDone,buffer));
                    workAvailable.Set();
                }

                // n.b. this was wrapped in an exception handler, but bug should be fixed now
            }
        }

        private void OnBufferDone(WaveOutBuffer buffer)
        {
            if (playbackState == PlaybackState.Playing)
            {
                if (!buffer.OnDone())
                {
                    playbackState = PlaybackState.Stopped;
                    RaisePlaybackStopped();
                }
            }
        }

        private void RaisePlaybackStopped()
        {
            if (PlaybackStopped != null)
            {
                PlaybackStopped(this, EventArgs.Empty);
            }
        }
    }



    class WaveOutAction
    {
        private WaveOutFunction function;


        private object data;

        public WaveOutAction(WaveOutFunction function, object data)
        {
            this.function = function;
            this.data = data;
        }
        
        public WaveOutFunction Function
        {
            get { return function; }
        }

        public object Data
        {
            get { return data; }
        }
    }

    enum WaveOutFunction
    {
        Init,
        Stop,
        BufferDone,
        Pause,
        Play,
        Resume,
        SetVolume,
        Exit
    }
}
