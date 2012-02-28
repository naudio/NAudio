using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.ComponentModel;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumMachineDemoViewModel : IDisposable, INotifyPropertyChanged
    {
        private IWavePlayer waveOut;
        private DrumPattern pattern;
        private DrumPatternSampleProvider patternSequencer;
        private int tempo;
        public ICommand PlayCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public DrumMachineDemoViewModel(DrumPattern pattern)
        {
            this.pattern = pattern;
            this.tempo = 100;
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
            this.patternSequencer = new DrumPatternSampleProvider(pattern);
            this.patternSequencer.Tempo = tempo;
            IWaveProvider wp = new SampleToWaveProvider(patternSequencer);
            waveOut.Init(wp);
            waveOut.Play();
        }

        private void Stop()
        {
            if (waveOut != null)
            {
                this.patternSequencer = null;
                waveOut.Dispose();
                waveOut = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public int Tempo
        {
            get
            {
                return tempo;
            }
            set
            {
                if (tempo != value)
                {
                    this.tempo = value;
                    if (this.patternSequencer != null)
                    {
                        this.patternSequencer.Tempo = value;
                    }
                    RaisePropertyChanged("Tempo");
                }
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
