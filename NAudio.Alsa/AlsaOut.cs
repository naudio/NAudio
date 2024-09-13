using System;
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
            AlsaInterop.PcmHwParamsFree(HwParams);
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
                    if ((error = AlsaInterop.PcmStart(Handle)) < 0)
                    {
                        var errorstring = AlsaInterop.ErrorString(error);
                        throw new Exception($"snd_pcm_start: {errorstring}");
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
            if ((error = AlsaInterop.AsyncAddPcmHandler(out IntPtr handler, Handle, callback, default)) != 0)
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
                ulong buffer_size = (ulong)waveBuffer.Length;
                GetHardwareParams();
                SetInterleavedAccess();
                SetFormat(waveProvider);
                SetPeriods(ref periods, ref dir);
                SetBufferSize(ref buffer_size);
                SetHardwareParams();
                GetSoftwareParams();
                AlsaInterop.PcmSwParamsSetStartThreshold(Handle, SwParams, buffer_size - PERIOD_SIZE);
                AlsaInterop.PcmSwParamsSetAvailMin(Handle, SwParams, PERIOD_SIZE);
                if ((error = AlsaInterop.PcmSwParams(Handle, SwParams)) < 0)
                {
                    AlsaInterop.PcmSwParamsFree(SwParams);
                    throw new NotSupportedException(AlsaInterop.ErrorString(error));
                }
                AlsaInterop.PcmSwParamsFree(SwParams);
                AlsaInterop.PcmPrepare(Handle);
                if (async)
                {
                    AlsaInterop.PcmWriteI(Handle, waveBuffer, 2 * PERIOD_SIZE);
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
                throw new NotSupportedException("Sample type not supported");
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
                if (!async) SwapBuffers();
                int error = AlsaInterop.PcmWriteI(Handle, waveBuffer, PERIOD_SIZE);
                BufferUpdate();
                if (error < 0)
                {
                    playbackState = PlaybackState.Stopped;
                    RaisePlaybackStopped(new Exception(AlsaInterop.ErrorString(error)));
                }
                avail = AlsaInterop.PcmAvailUpdate(Handle);
            }
        }
        private void SwapBuffers()
        {
            bufferNum = ++bufferNum % NumberOfBuffers;
            waveBuffer = buffers[bufferNum];
        }
        private void Callback(IntPtr callback)    
        {
            WritePcm();
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
