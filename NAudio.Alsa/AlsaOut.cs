using System;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NAudio.Wave.SampleProviders;
namespace NAudio.Wave
{

    public class AlsaOut : AlsaPcm, IWavePlayer
    {
        private IWaveProvider sourceStream;
        private PlaybackState playbackState;
        private byte[] waveBuffer;
        public WaveFormat OutputWaveFormat { get; private set; }
        public event EventHandler<StoppedEventArgs> PlaybackStopped;
        public float Volume 
        {
            get => throw new NotImplementedException("");
            set => throw new NotImplementedException("");
        }
        public PlaybackState PlaybackState
        {
            get => playbackState;
            set => throw new NotImplementedException("");
        }
        public void Dispose()
        {
            AlsaDriverExt.PcmClose(Handle);
        }

        public void Init(IWaveProvider waveProvider)
        {
            InitPlayback(waveProvider);
        }
        public void Play()
        {
            int error;
            if ((error = AlsaDriverExt.PcmStart(Handle)) == 0)
            {
                return;
            }
            Console.WriteLine(AlsaDriverExt.ErrorString(error));
        }
        public void Stop()
        {

        }
        public void Pause()
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
            device = default;
            int error;
            device = new AlsaOut(); 
            if ((error = AlsaDriverExt.PcmOpen(out device.Handle, pcm_name, PCMStream.SND_PCM_STREAM_PLAYBACK, 0)) == 0)
            {
                return true;
            }
            Console.WriteLine(AlsaDriverExt.ErrorString(error));
            return false;
        }
        public void InitPlayback(IWaveProvider waveProvider)
        {
            int error;
            int dir = 0;
            uint periods = PERIOD_QUANTITY;
            ulong buffer_size = PERIOD_SIZE * PERIOD_QUANTITY;
            if (isInitialized)
            {
                throw new InvalidOperationException("Already initialized this PCM");
            }
            isInitialized = true;
            if (waveProvider != null)
            {
                sourceStream = waveProvider;
                int desiredSampleRate = waveProvider.WaveFormat.SampleRate;
                AlsaDriverExt.PcmHwParamsMalloc(out IntPtr hwparams);    
                AlsaDriverExt.PcmHwParamsAny(Handle, hwparams);
                if ((error = AlsaDriverExt.PcmHwParamsTestAccess(Handle, hwparams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED)) != 0)
                {
                    throw new Exception(AlsaDriverExt.ErrorString(error));
                }
                AlsaDriverExt.PcmHwParamsSetAccess(Handle, hwparams, PCMAccess.SND_PCM_ACCESS_RW_INTERLEAVED);
                var format = AlsaDriver.GetFormat(waveProvider.WaveFormat);
                Console.WriteLine(waveProvider.WaveFormat);
                if (AlsaDriverExt.PcmHwParamsTestFormat(Handle, hwparams, format) == 0)
                {
                    OutputWaveFormat = waveProvider.WaveFormat;
                    AlsaDriverExt.PcmHwParamsSetFormat(Handle, hwparams, format);
                }
                else if ((error = AlsaDriverExt.PcmHwParamsTestFormat(Handle, hwparams, PCMFormat.SND_PCM_FORMAT_S16_LE)) == 0)
                {
                    AlsaDriverExt.PcmHwParamsSetFormat(Handle, hwparams, PCMFormat.SND_PCM_FORMAT_S16_LE);
                    sourceStream = new SampleToWaveProvider16(new WaveToSampleProvider(waveProvider));
                    OutputWaveFormat = sourceStream.WaveFormat;
                }
                else
                {
                    AlsaDriverExt.PcmHwParamsFree(hwparams);
                    Console.WriteLine(AlsaDriverExt.ErrorString(error));
                    throw new NotSupportedException("Sample type not supported");
                }

                if ((AlsaDriverExt.PcmHwParamsSetRate(Handle, hwparams, (uint)waveProvider.WaveFormat.SampleRate, 0)) != 0)
                {
                    AlsaDriverExt.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Sample rate not supported.");
                }
                if ((AlsaDriverExt.PcmHwParamsSetChannels(Handle, hwparams, (uint)waveProvider.WaveFormat.Channels)) != 0)
                {
                    AlsaDriverExt.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Number of channels not supported.");
                }
                if ((AlsaDriverExt.PcmHwParamsSetPeriodsNear(Handle, hwparams, ref periods, ref dir)) != 0)
                {
                    AlsaDriverExt.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Periods not supported");
                }
                if ((AlsaDriverExt.PcmHwParamsSetBufferSizeNear(Handle, hwparams, ref buffer_size)) != 0)
                {
                    AlsaDriverExt.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException("Buffer Size not supported");
                }
                if ((error = AlsaDriverExt.PcmHwParams(Handle, hwparams)) != 0)
                {
                    AlsaDriverExt.PcmHwParamsFree(hwparams);
                    throw new NotSupportedException(AlsaDriverExt.ErrorString(error));
                }
                AlsaDriverExt.PcmHwParamsFree(hwparams);
                AlsaDriverExt.PcmSwParamsMalloc(out IntPtr swparams);
                AlsaDriverExt.PcmSwParamsCurrent(Handle, swparams);
                AlsaDriverExt.PcmSwParamsSetStartThreshold(Handle, swparams, buffer_size - PERIOD_SIZE);
                AlsaDriverExt.PcmSwParamsSetAvailMin(Handle, swparams, PERIOD_SIZE);
                if ((error = AlsaDriverExt.PcmSwParams(Handle, swparams)) < 0)
                {
                    AlsaDriverExt.PcmSwParamsFree(swparams);
                    throw new NotSupportedException(AlsaDriverExt.ErrorString(error));
                }
                AlsaDriverExt.PcmSwParamsFree(swparams);
                Console.WriteLine(OutputWaveFormat);
                waveBuffer = new byte[buffer_size];
                AlsaDriverExt.PcmPrepare(Handle);
                AlsaDriverExt.PcmWriteI(Handle, waveBuffer, 2 * PERIOD_SIZE);
                AlsaDriverExt.AsyncAddPcmHandler(out IntPtr handler, Handle, PcmCallbackHandler, default);
            }
        }
        private void PcmCallbackHandler(IntPtr callback)    
        {
            ulong avail = AlsaDriverExt.PcmAvailUpdate(Handle);
            int error;
            BufferUpdate();
            while (avail >= PERIOD_SIZE)
            {
                Console.WriteLine("avail:{0} read:{1}", avail, "0");
                AlsaDriverExt.PcmWriteI(Handle, waveBuffer, PERIOD_SIZE);
                avail = AlsaDriverExt.PcmAvailUpdate(Handle);
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
            }
        }
    }
}