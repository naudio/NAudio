using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.ComponentModel;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumMachineDemoViewModel : ViewModelBase, IDisposable
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
            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
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
            waveOut.Init(patternSequencer);
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
                    OnPropertyChanged("Tempo");
                }
            }
        }

    }
}
