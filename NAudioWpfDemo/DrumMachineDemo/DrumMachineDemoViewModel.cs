using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumMachineDemoViewModel : IDisposable
    {
        private IWavePlayer waveOut;
        private DrumPattern pattern;
        public ICommand PlayCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public DrumMachineDemoViewModel(DrumPattern pattern)
        {
            this.pattern = pattern;
            PlayCommand = new RelayCommand(
               () => this.Play(),
               () => true);
            StopCommand = new RelayCommand(
               () => this.Stop(),
               () => true);
        }

        private void Play()
        {
            if (waveOut != null)
            {
                Stop();
            }
            waveOut = new WaveOut();
            ISampleProvider sp = new PatternSequencer(pattern);
            IWaveProvider wp = new SampleToWaveProvider(sp);
            waveOut.Init(wp);
            waveOut.Play();
        }

        private void Stop()
        {
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
        }

        public void Dispose()
        {
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
        }
    }
}
