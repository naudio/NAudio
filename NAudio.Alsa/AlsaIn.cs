using System;
using NAudio.Wave.Alsa;
using System.Threading.Tasks;
using System.Linq;

namespace NAudio.Wave
{
    public class AlsaIn : AlsaPcm, IWaveIn
    {
        private bool recording;
        private AlsaInterop.PcmCallback callback;
        public void StartRecording()
        {
            if (recording)
            {
                throw new InvalidOperationException("Already recording");
            }
            InitRecord(WaveFormat);
            int error;
            recording = true;
            if ((error = AlsaInterop.PcmStart(Handle)) < 0)
            {
                throw new AlsaException("snd_pcm_start", error);
            }
            else if (!Async)
            {
                RecordPcmSync();
            }
        }    
        public void StopRecording()
        {
            recording = false;
            int error;
            if ((error = AlsaInterop.PcmDrop(Handle)) < 0)
            {
                RaiseRecordingStopped(new AlsaException(error));
            }
            else
            {
                RaiseRecordingStopped(null);
            }
        }
        public WaveFormat WaveFormat { get; set;}
        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            AlsaInterop.PcmHwParamsFree(HwParams);
            AlsaInterop.PcmSwParamsFree(SwParams);
            AlsaInterop.PcmClose(Handle);
        }
        public AlsaIn(string pcm_name)
        {
            int error;
            if ((error = AlsaInterop.PcmOpen(out Handle, pcm_name, PCMStream.SND_PCM_STREAM_CAPTURE, 0)) < 0)
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
        }
        private void InitRecord(WaveFormat waveFormat)
        {
            GetHardwareParams();
            int error;
            int dir = 0;
            uint periods = PERIOD_QUANTITY;
            if (isInitialized)
            {
                throw new InvalidOperationException("Already initialized");
            }
            isInitialized = true;
            InitBuffers();
            ulong buffer_size = (ulong)WaveBuffer.Length;
            SetInterleavedAccess();
            SetFormat(waveFormat);
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
            AlsaInterop.PcmPrepare(Handle);
        }
        public AlsaIn() : this("default")
        {
        }
        ~AlsaIn()
        {
            Dispose();
        }
        private void Callback(IntPtr cb_info)
        {
            ReadPcm();
        }
        private void ReadPcm()
        {
            ulong avail = AlsaInterop.PcmAvailUpdate(Handle);
            int bits_per_frame = WaveFormat.BitsPerSample * WaveFormat.Channels;
            int frame_bytes = bits_per_frame / 8;
            while (avail >= PERIOD_SIZE)
            {
                int frames = AlsaInterop.PcmReadI(Handle, WaveBuffer, PERIOD_SIZE);
                if (frames < 0)
                {
                    recording = false;
                    RaiseRecordingStopped(new AlsaException(frames));
                }
                if (!Async) SwapBuffers();
                RaiseDataAvailable(WaveBuffer, frames * frame_bytes);
                avail = AlsaInterop.PcmAvailUpdate(Handle);
            }
        }
        private void RecordPcmSync()
        {
            Task.Run(() => {
                while (recording)
                {
                    ReadPcm();
                }
            });
        }
        private void SetFormat(WaveFormat format)
        {
            int error;
            if (GetValidWaveFormats().Contains(format))
            {
                WaveFormat = format;
            }
            else 
            {
                throw new NotSupportedException($"{format} not supported");
            }
            var sampleformat = AlsaDriver.GetFormat(WaveFormat);
            if ((error = AlsaInterop.PcmHwParamsSetFormat(Handle, HwParams, sampleformat)) < 0)
            {
                throw new AlsaException(error);
            }
            int desiredSampleRate = WaveFormat.SampleRate;
            SetSampleRate((uint)desiredSampleRate);
            SetNumberOfChannels((uint)WaveFormat.Channels);
        }
        private void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                handler(this, new StoppedEventArgs(e));
            }
        }
        private void RaiseDataAvailable(byte[] buffer, int bytes)
        {
            DataAvailable?.Invoke(this, new WaveInEventArgs(buffer, bytes));
        }
    }
}