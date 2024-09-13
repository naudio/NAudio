using System;
using NAudio.Wave.Alsa;
using NAudio.Wave.SampleProviders;
using System.Threading.Tasks;

namespace NAudio.Wave
{
    public class AlsaIn : AlsaPcm, IWaveIn
    {
        public void StartRecording(){}    
        public void StopRecording(){}
        public WaveFormat WaveFormat { get; set;}
        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;
        public void Dispose(){}
    }
}