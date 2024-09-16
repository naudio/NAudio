using System;
using NAudio.Wave.Alsa;
using NAudio.Wave.SampleProviders;
using System.Threading.Tasks;
namespace NAudio.Wave
{

    public class AlsaOut : AlsaPcm, IWavePlayer
    {
        private IWaveProvider sourceStream;
        private PlaybackState playbackState;
        private readonly AlsaInterop.PcmCallback callback;
        public WaveFormat OutputWaveFormat { get; private set; }
        public event EventHandler<StoppedEventArgs> PlaybackStopped;
        public float Volume 
        {
            get => 1.0f;
            set => throw new NotImplementedException("");
        }
        public PlaybackState PlaybackState
        {
            get => playbackState;
        }
        public bool HasReachedEnd { get; private set; }
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            AlsaInterop.PcmClose(Handle);
            if (HwParams != default) AlsaInterop.PcmHwParamsFree(HwParams);
            if (SwParams != default) AlsaInterop.PcmSwParamsFree(SwParams);
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
                    throw new AlsaException(error);
                }
                if (!Async)
                {
                    PlayPcmSync();
                }
            }
            else if (playbackState != PlaybackState.Playing)
            {
                if (Async) 
                {
                    int error;
                    if ((error = AlsaInterop.PcmStart(Handle)) < 0)
                    {
                        error = AlsaInterop.PcmRecover(Handle, error, 0);
                    }
                    if (error < 0)
                    {
                        throw new AlsaException("snd_pcm_start", error);
                    }
                    playbackState = PlaybackState.Playing;
                    HasReachedEnd = false;
                    return;
                }
                else 
                {
                    playbackState = PlaybackState.Playing;
                    HasReachedEnd = false;
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
                    throw new AlsaException(error);
                }
            }
        }
        public AlsaOut(string pcm_name)
        {
            int error;
            if ((error = AlsaInterop.PcmOpen(out Handle, pcm_name, PCMStream.SND_PCM_STREAM_PLAYBACK, 0)) < 0)
            {
                throw new AlsaException("snd_pcm_open", error);
            }
            callback = Callback;
            ulong buffer_size = PERIOD_SIZE * PERIOD_QUANTITY;
            if ((error = AlsaInterop.AsyncAddPcmHandler(out IntPtr handler, Handle, callback, default)) != 0)
            {
                Async = false;
            }
            else
            {
                Async = true;
            }
            GetHardwareParams();
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
                InitBuffers();
                ulong buffer_size = (ulong)WaveBuffer.Length;
                SetInterleavedAccess();
                SetFormat(waveProvider);
                SetPeriods(ref periods, ref dir);
                SetBufferSize(ref buffer_size);
                SetHardwareParams();
                GetSoftwareParams();
                if ((error = AlsaInterop.PcmSwParamsSetStartThreshold(Handle, SwParams, buffer_size - PERIOD_SIZE)) < 0)
                {
                    throw new AlsaException(error);
                }
                if ((error = AlsaInterop.PcmSwParamsSetAvailMin(Handle, SwParams, PERIOD_SIZE)) < 0)
                {
                    throw new AlsaException(error);
                }
                SetSoftwareParams();
                _ = State; // idk... it doesn't work without this
                if (Async)
                {
                    AlsaInterop.PcmWriteI(Handle, WaveBuffer, 2 * PERIOD_SIZE);
                }
            }
        }
        public void SetFormat(IWaveProvider waveProvider)
        {
            int error;
            var format = AlsaDriver.GetFormat(waveProvider.WaveFormat); 
            if (AlsaInterop.PcmHwParamsTestFormat(Handle, HwParams, format) == 0)
            {
                OutputWaveFormat = waveProvider.WaveFormat;
                AlsaInterop.PcmHwParamsSetFormat(Handle, HwParams, format);
            }
            else if ((error = AlsaInterop.PcmHwParamsTestFormat(Handle, HwParams, PCMFormat.SND_PCM_FORMAT_S32_LE)) == 0)
            {
                AlsaInterop.PcmHwParamsSetFormat(Handle, HwParams, PCMFormat.SND_PCM_FORMAT_S32_LE);
                sourceStream = new SampleToWaveProvider32(new WaveToSampleProvider(waveProvider));
                OutputWaveFormat = sourceStream.WaveFormat;
            }
            else if ((error = AlsaInterop.PcmHwParamsTestFormat(Handle, HwParams, PCMFormat.SND_PCM_FORMAT_S16_LE)) == 0)
            {
                AlsaInterop.PcmHwParamsSetFormat(Handle, HwParams, PCMFormat.SND_PCM_FORMAT_S16_LE);
                sourceStream = new SampleToWaveProvider16(new WaveToSampleProvider(waveProvider));
                OutputWaveFormat = sourceStream.WaveFormat;
            }
            else
            {
                AlsaInterop.PcmHwParamsFree(HwParams);
                throw new AlsaException(error);
            }
            int desiredSampleRate = waveProvider.WaveFormat.SampleRate;
            SetSampleRate((uint)desiredSampleRate);
            SetNumberOfChannels((uint)waveProvider.WaveFormat.Channels);
        }
        private void PlayPcmSync()
        {
            Task.Run(() => {
                while (playbackState == PlaybackState.Playing)
                {
                    WritePcm();
                }
            });
        }
        private void WritePcm()
        {
            ulong avail = AlsaInterop.PcmAvailUpdate(Handle);
            while (avail >= PERIOD_SIZE)
            {
                if (!Async) SwapBuffers();
                int error = AlsaInterop.PcmWriteI(Handle, WaveBuffer, PERIOD_SIZE);
                BufferUpdate();
                if (error < 0)
                {
                    if ((error = AlsaInterop.PcmRecover(Handle, error, 0)) < 0)
                    {
                        RaisePlaybackStopped(new AlsaException(error));
                    }
                }
                avail = AlsaInterop.PcmAvailUpdate(Handle);
            }
        }
        private void Callback(IntPtr callback)    
        {
            WritePcm();
        }
        private void BufferUpdate()
        {
            int read = sourceStream.Read(WaveBuffer, 0, WaveBuffer.Length);
            if (read < WaveBuffer.Length)
            {
                Array.Clear(WaveBuffer, read, WaveBuffer.Length - read);
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
