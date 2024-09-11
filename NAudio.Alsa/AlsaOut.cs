using System;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NAudio.Wave.SampleProviders;
using System.Threading.Tasks;
namespace NAudio.Wave
{

    public class AlsaOut : AlsaPcm, IWavePlayer
    {
        private bool async;
        private IWaveProvider sourceStream;
        private PlaybackState playbackState;
        private AlsaInterop.PcmCallback callback;
        private byte[] waveBuffer;
        private byte[][] buffers;
        private int bufferNum;
        public WaveFormat OutputWaveFormat { get; private set; }
        public event EventHandler<StoppedEventArgs> PlaybackStopped;
        public int NumberOfBuffers { get; set; } = 2;
        public float Volume 
        {
            get => throw new NotImplementedException("");
            set => throw new NotImplementedException("");
        }
        public PlaybackState PlaybackState
        {
            get => playbackState;
        }
        public bool HasReachedEnd { get; private set; }
        public void Dispose()
        {
            AlsaInterop.PcmClose(Handle);
        }

        public void Init(IWaveProvider waveProvider)
        {
            InitPlayback(waveProvider);
        }
        public void Play()
        {
            if (playbackState == PlaybackState.Paused)
            {
                playbackState = PlaybackState.Playing;
                int error;
                if ((error = AlsaInterop.PcmPause(Handle, 0)) < 0)
                {
                    throw new Exception(AlsaInterop.ErrorString(error));
                }
                if (!async)
                {
                    PlayPcmSync();
                }
            }
            else if (playbackState != PlaybackState.Playing)
            {
                if (async) 
                {
                    int error;
                    if ((error = AlsaInterop.PcmStart(Handle)) == 0)
                    {
                        playbackState = PlaybackState.Playing;
                        HasReachedEnd = false;
                        return;
                    }
                }
                else 
                {
                    playbackState = PlaybackState.Playing;
                    BufferUpdate();
                    PlayPcmSync();
                }
            }
        }
        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            AlsaInterop.PcmDrop(Handle);
            HasReachedEnd = false;
            RaisePlaybackStopped(null); 
        }
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
                int error;
                if ((error = AlsaInterop.PcmPause(Handle, 1)) < 0)
                {
                    throw new Exception(AlsaInterop.ErrorString(error));
                }
            }
        }
        public AlsaOut(string pcm_name)
        {
            int error;
            if ((error = AlsaInterop.PcmOpen(out Handle, pcm_name, PCMStream.SND_PCM_STREAM_PLAYBACK, 0)) < 0)
            {
                var errorstring = AlsaInterop.ErrorString(error);
                throw new Exception($"snd_pcm_open: {errorstring}");
            }
            callback = Callback;
            ulong buffer_size = PERIOD_SIZE * PERIOD_QUANTITY;
            if (true || (error = AlsaInterop.AsyncAddPcmHandler(out IntPtr handler, Handle, Callback, default)) != 0)
            {
                async = false;
                buffers = new byte[NumberOfBuffers][];
                for (int i = 0; i < NumberOfBuffers; i++)
                {
                    buffers[i] = new byte[buffer_size];    
                }
                waveBuffer = buffers[bufferNum];
            }
            else
            {
                waveBuffer = new byte[buffer_size];
                async = true;
            }
        }
        public AlsaOut() : this("default")
        {
        }
        ~AlsaOut()
        {
            Dispose();
        } 
        public static bool Create(AlsaCard card, int device_num, out AlsaOut device)
        {
            device = null;
            return false;
        }
        public static bool Create(string pcm_name, out AlsaOut device)
        {
            device = null;
            try
            {
                device = new AlsaOut(pcm_name);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void InitPlayback(IWaveProvider waveProvider)
        {
            int error;
            int dir = 0;
            uint periods = PERIOD_QUANTITY;
            if (isInitialized)
            {
                throw new InvalidOperationException("Already initialized this PCM");
            }
            isInitialized = true;
            if (waveProvider != null)
            {
                sourceStream = waveProvider;
                int desiredSampleRate = waveProvider.WaveFormat.SampleRate;
                AlsaInterop.PcmHwParamsMalloc(out IntPtr hwparams);    
                AlsaInterop.PcmHwParamsAny(Handle, hwparams);
                if ((error = AlsaInterop.PcmHwParamsTestAccess(Handle, hwparams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED)) != 0)
                {
                    throw new Exception(AlsaInterop.ErrorString(error));
                }
                AlsaInterop.PcmHwParamsSetAccess(Handle, hwparams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED);
                var format = AlsaDriver.GetFormat(waveProvider.WaveFormat);
                if (AlsaInterop.PcmHwParamsTestFormat(Handle, hwparams, format) == 0)
                {
                    OutputWaveFormat = waveProvider.WaveFormat;
                    AlsaInterop.PcmHwParamsSetFormat(Handle, hwparams, format);
                }
                else if ((error = AlsaInterop.PcmHwParamsTestFormat(Handle, hwparams, PCMFormat.SND_PCM_FORMAT_S32_LE)) == 0)
                {
                    AlsaInterop.PcmHwParamsSetFormat(Handle, hwparams, PCMFormat.SND_PCM_FORMAT_S32_LE);
                    sourceStream = new SampleToWaveProvider32(new WaveToSampleProvider(waveProvider));
                    OutputWaveFormat = sourceStream.WaveFormat;
                }
                else if ((error = AlsaInterop.PcmHwParamsTestFormat(Handle, hwparams, PCMFormat.SND_PCM_FORMAT_S16_LE)) == 0)
                {
                    AlsaInterop.PcmHwParamsSetFormat(Handle, hwparams, PCMFormat.SND_PCM_FORMAT_S16_LE);
                    sourceStream = new SampleToWaveProvider16(new WaveToSampleProvider(waveProvider));
                    OutputWaveFormat = sourceStream.WaveFormat;
                }
                else
                {
                    AlsaInterop.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Sample type not supported");
                }

                if ((AlsaInterop.PcmHwParamsSetRate(Handle, hwparams, (uint)waveProvider.WaveFormat.SampleRate, 0)) != 0)
                {
                    AlsaInterop.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Sample rate not supported.");
                }
                if ((AlsaInterop.PcmHwParamsSetChannels(Handle, hwparams, (uint)waveProvider.WaveFormat.Channels)) != 0)
                {
                    AlsaInterop.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Number of channels not supported.");
                }
                if ((AlsaInterop.PcmHwParamsSetPeriodsNear(Handle, hwparams, ref periods, ref dir)) != 0)
                {
                    AlsaInterop.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Periods not supported");
                }
                ulong buffer_size = (ulong)waveBuffer.Length;
                if ((AlsaInterop.PcmHwParamsSetBufferSizeNear(Handle, hwparams, ref buffer_size)) != 0)
                {
                    AlsaInterop.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Buffer Size not supported");
                }
                if ((error = AlsaInterop.PcmHwParams(Handle, hwparams)) != 0)
                {
                    AlsaInterop.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException(AlsaInterop.ErrorString(error));
                }
                AlsaInterop.PcmHwParamsFree(hwparams);
                AlsaInterop.PcmSwParamsMalloc(out IntPtr swparams);
                AlsaInterop.PcmSwParamsCurrent(Handle, swparams);
                AlsaInterop.PcmSwParamsSetStartThreshold(Handle, swparams, buffer_size - PERIOD_SIZE);
                AlsaInterop.PcmSwParamsSetAvailMin(Handle, swparams, PERIOD_SIZE);
                if ((error = AlsaInterop.PcmSwParams(Handle, swparams)) < 0)
                {
                    AlsaInterop.PcmSwParamsFree(swparams);
                    throw new NotSupportedException(AlsaInterop.ErrorString(error));
                }
                AlsaInterop.PcmSwParamsFree(swparams);
                AlsaInterop.PcmPrepare(Handle);
            }
        }
        private void PlayPcmSync()
        {
            Task.Run(() => {
                while (playbackState == PlaybackState.Playing)
                {
                    ulong avail = AlsaInterop.PcmAvailUpdate(Handle);
                    while (avail >= PERIOD_SIZE)
                    {
                        SwapBuffers();
                        int error = AlsaInterop.PcmWriteI(Handle, waveBuffer, PERIOD_SIZE);
                        BufferUpdate();
                        if (error < 0)
                        {
                            throw new Exception(AlsaInterop.ErrorString(error));
                        }
                        avail = AlsaInterop.PcmAvailUpdate(Handle);
                    }
                }
            });
        }
        private void SwapBuffers()
        {
            bufferNum = ++bufferNum % NumberOfBuffers;
            waveBuffer = buffers[bufferNum];
        }
        private void Callback(IntPtr callback)    
        {
            ulong avail = AlsaInterop.PcmAvailUpdate(Handle);
            int error;
            while (avail >= PERIOD_SIZE)
            {
                if ((error = AlsaInterop.PcmWriteI(Handle, waveBuffer, PERIOD_SIZE)) < 0)
                {
                }
                BufferUpdate();
                avail = AlsaInterop.PcmAvailUpdate(Handle);
            } 
        }
        private void BufferUpdate()
        {
            int read = sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
            if (read < waveBuffer.Length)
            {
                Array.Clear(waveBuffer, read, waveBuffer.Length - read);
            }
            if (read == 0)
            {
                Stop();
                HasReachedEnd = true;
            }
        }
        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                handler(this, new StoppedEventArgs(e));
            }
        }
    }
}
