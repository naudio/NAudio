using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.DrumMachineDemo
{
    class DrumMachineDemoViewModel : ViewModelBase, IDisposable
    {
        private const int OfflineRenderSeconds = 8;

        private IWavePlayer waveOut;
        private readonly DrumPattern pattern;
        private DrumPatternSampleProvider engine;
        private int tempo;
        private double swing;

        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RenderToWavCommand { get; }

        public DrumMachineDemoViewModel(DrumPattern pattern)
        {
            this.pattern = pattern;
            tempo = 100;
            swing = 0.0;
            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
            RenderToWavCommand = new DelegateCommand(RenderToWav);
        }

        private void Play()
        {
            if (waveOut != null) Stop();
            waveOut = new WaveOut();
            engine = new DrumPatternSampleProvider(pattern, new DrumKit(), tempo) { Swing = swing };
            engine.Transport.Play();
            waveOut.Init(engine);
            waveOut.Play();
        }

        private void Stop()
        {
            if (waveOut != null)
            {
                engine?.Transport.Stop();
                waveOut.Dispose();
                waveOut = null;
                engine = null;
            }
        }

        private void RenderToWav()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "WAV files (*.wav)|*.wav",
                DefaultExt = ".wav",
                FileName = "drum-pattern.wav",
            };
            if (dialog.ShowDialog() != true) return;

            var offline = new DrumPatternSampleProvider(pattern, new DrumKit(), tempo) { Swing = swing };
            offline.Transport.Play();

            long totalSamples = (long)OfflineRenderSeconds * offline.WaveFormat.SampleRate * offline.WaveFormat.Channels;
            var buffer = new float[offline.WaveFormat.SampleRate * offline.WaveFormat.Channels];
            try
            {
                using var writer = new WaveFileWriter(dialog.FileName, offline.WaveFormat);
                long written = 0;
                while (written < totalSamples)
                {
                    int toRead = (int)Math.Min(buffer.Length, totalSamples - written);
                    int read = offline.Read(buffer.AsSpan(0, toRead));
                    if (read == 0) break;
                    writer.WriteSamples(buffer, 0, read);
                    written += read;
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Render failed: {ex.Message}", "Render to WAV", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose() => Stop();

        public int Tempo
        {
            get => tempo;
            set
            {
                if (tempo == value) return;
                tempo = value;
                if (engine != null) engine.Tempo = value;
                OnPropertyChanged(nameof(Tempo));
            }
        }

        /// <summary>Swing amount, 0..0.5.</summary>
        public double Swing
        {
            get => swing;
            set
            {
                if (swing == value) return;
                swing = value;
                if (engine != null) engine.Swing = value;
                OnPropertyChanged(nameof(Swing));
            }
        }
    }
}
