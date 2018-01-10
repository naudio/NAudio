using System;
using System.Linq;
using System.Windows.Input;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumMachineDemoViewModel : ViewModelBase, IDisposable
    {
        private IWavePlayer waveOut;
        private readonly DrumPattern pattern;
        private DrumPatternSampleProvider patternSequencer;
        private int tempo;
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }

        public DrumMachineDemoViewModel(DrumPattern pattern)
        {
            this.pattern = pattern;
            tempo = 100;
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
            patternSequencer = new DrumPatternSampleProvider(pattern);
            patternSequencer.Tempo = tempo;
            waveOut.Init(patternSequencer);
            waveOut.Play();
        }

        private void Stop()
        {
            if (waveOut != null)
            {
                patternSequencer = null;
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
            get => tempo;
            set
            {
                if (tempo == value) return;
                tempo = value;
                if (patternSequencer != null)
                {
                    patternSequencer.Tempo = value;
                }
                OnPropertyChanged("Tempo");
            }
        }

    }
}
