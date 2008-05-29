using System;
using System.Windows.Forms;
using Microsoft.DirectX.DirectSound;
using System.Diagnostics;

namespace NAudio.Wave
{
    /// <summary>
    /// DirectSound output support
    /// </summary>
    public class DirectSoundOut : IWavePlayer
    {
        private SecondaryBuffer buffer = null;
        private Device device = null;
        private PlaybackState playbackState;
        private WaveStream waveStream;

        private int bufferSize;
        private System.Timers.Timer timer;
        private int nextWrite;
        private int desiredLatency;
        private int readSize;

        /// <summary>
        /// Creates a new DirectSound wave player
        /// </summary>
        /// <param name="owner">The owning form</param>
        /// <param name="desiredLatency">Desired latency in ms</param>
        public DirectSoundOut(System.Windows.Forms.Control owner, int desiredLatency)
        {
            this.desiredLatency = desiredLatency;
            device = new Device();
            device.SetCooperativeLevel(owner, CooperativeLevel.Normal); //CooperativeLevel.Priority);
        }

        /// <summary>
        /// Initialises the wave player
        /// </summary>
        /// <param name="waveStream">The wave stream to be played</param>
        public void Init(WaveStream waveStream)
        {
            Microsoft.DirectX.DirectSound.WaveFormat dsWaveFormat =
                new Microsoft.DirectX.DirectSound.WaveFormat();
            dsWaveFormat.FormatTag = WaveFormatTag.Pcm;
            dsWaveFormat.Channels = (short)waveStream.WaveFormat.Channels;
            dsWaveFormat.BitsPerSample = (short)waveStream.WaveFormat.BitsPerSample;
            dsWaveFormat.SamplesPerSecond = waveStream.WaveFormat.SampleRate;
            dsWaveFormat.BlockAlign = (short)waveStream.WaveFormat.BlockAlign;
            dsWaveFormat.AverageBytesPerSecond = waveStream.WaveFormat.AverageBytesPerSecond;

            readSize = waveStream.GetReadSize(desiredLatency);
            BufferDescription bufferDescription = new BufferDescription(dsWaveFormat);
            bufferDescription.BufferBytes = dsWaveFormat.AverageBytesPerSecond;
            bufferDescription.ControlVolume = true;
            bufferDescription.GlobalFocus = true;
            bufferDescription.ControlPan = true;
            bufferDescription.ControlEffects = false;
            

            this.waveStream = waveStream;
            //waveFormatStream.Position = 46;
            buffer = new SecondaryBuffer(bufferDescription, device);
            bufferSize = buffer.Caps.BufferBytes;
            
            timer = new System.Timers.Timer(desiredLatency / 2);
            timer.Enabled = false;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);


        }

        /// <summary>
        /// Finalized
        /// </summary>
        ~DirectSoundOut()
        {
            Dispose();
        }

        /// <summary>
        /// Closes this output device and cleans up resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
            if (device != null)
            {
                device.Dispose();
                device = null;
            }
            GC.SuppressFinalize(this);
        }

        /*
        public int GetBufferedSize()
        {
            int played = GetPlayedSize();
            return played > 0 && played < bufferSize ? bufferSize - played : 0;
        }*/




        /*private int BytesToMs(int bytes)
        {
            return bytes * 1000 / buffer.Format.AverageBytesPerSecond;
        }*/

        private int MsToBytes(int ms)
        {
            int bytes = ms * buffer.Format.AverageBytesPerSecond / 1000;
            bytes -= bytes % buffer.Format.BlockAlign;
            return bytes;
        }

        private void Feed(int bytes)
        {
            Debug.Assert(bytes >= 0);
            bytes -= bytes % waveStream.BlockAlign;

            // limit latency to some milliseconds
            int toCopy = Math.Min(bytes, MsToBytes(desiredLatency));
            //int toCopy = readSize; //(bytes < readSize / 2) ? readSize / 2 : readSize;

            //Console.WriteLine("Feed {0} {1}",bytes,toCopy);


            if (toCopy > 0)
            {
                // restore buffer
                if (buffer.Status.BufferLost)
                    buffer.Restore();

                // copy data to the buffer
                buffer.Write(nextWrite, waveStream, toCopy, LockFlag.None);


                nextWrite += toCopy;
                if (nextWrite >= bufferSize)
                    nextWrite -= bufferSize;

            }
        }

        private int GetPlayedSize()
        {
            int pos = buffer.PlayPosition;

            return pos < nextWrite ? pos + bufferSize - nextWrite : pos - nextWrite;
        }

        private bool stopNextTime = false;

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            if (stopNextTime)
            {
                Stop();
                stopNextTime = false;
                return;
            }

            if (waveStream.Position >= waveStream.Length)
            {
                // one more timer period allows the buffer to play
                // could be cleverer, and wait for two
                stopNextTime = true;
            }
            else
            {
                //int playedSize = GetPlayedSize();
                stopNextTime = false;

                int leftToPlay = nextWrite - buffer.PlayPosition;
                if (leftToPlay < 0)
                    leftToPlay += bufferSize;
                if (leftToPlay < readSize)
                    Feed(readSize - leftToPlay);
                else
                    Console.WriteLine("Still a buffer full left {0}", leftToPlay);
            }
        }

        /// <summary>
        /// Volume, 1.0 is full scale
        /// </summary>
        public float Volume
        {
            get
            {
                return 1 + (buffer.Volume) / 10000.0f;
            }
            set
            {
                int intVol = (int)((value - 1) * 10000.0f);
                buffer.Volume = intVol;
            }
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }

        /// <summary>
        /// Begin playing
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                if (PlaybackState == PlaybackState.Stopped)
                {
                    buffer.SetCurrentPosition(0);
                    nextWrite = 0;
                    Feed(bufferSize);
                    Feed(bufferSize);
                }
                playbackState = PlaybackState.Playing;
                timer.Enabled = true;
                buffer.Play(0, BufferPlayFlags.Looping);
            }
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            if (timer != null)
                timer.Enabled = false;
            if (buffer != null)
                buffer.Stop();
        }

        /// <summary>
        /// Pause playback
        /// </summary>
        public void Pause()
        {
            playbackState = PlaybackState.Paused;
            if (timer != null)
                timer.Enabled = false;
            if (buffer != null)
                buffer.Stop();
        }
    }
}
