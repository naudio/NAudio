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
        public DrumMachineDemoViewModel()
        {
            PlayCommand = new RelayCommand(
               () => this.Play(),
               () => true);
        }

        public ICommand PlayCommand { get; private set; }

        private void Play()
        {
            waveOut = new WaveOut();
            ISampleProvider sp = new PatternSequencer();
            IWaveProvider wp = new SampleToWaveProvider(sp);
            waveOut.Init(wp);
            waveOut.Play();
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
