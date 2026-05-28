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
        private DrumPatternSampleProvider legacyEngine;
        private SequencedDrumPatternSampleProvider sequencingEngine;
        private int tempo;
        private double swing;
        private bool useLegacyEngine;

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
            if (useLegacyEngine)
            {
                legacyEngine = new DrumPatternSampleProvider(pattern) { Tempo = tempo };
                waveOut.Init(legacyEngine);
            }
            else
            {
                sequencingEngine = new SequencedDrumPatternSampleProvider(pattern, new DrumKit(), tempo) { Swing = swing };
                sequencingEngine.Transport.Play();
                waveOut.Init(sequencingEngine);
            }
            waveOut.Play();
        }

        private void Stop()
        {
            if (waveOut != null)
            {
                sequencingEngine?.Transport.Stop();
                waveOut.Dispose();
                waveOut = null;
                legacyEngine = null;
                sequencingEngine = null;
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

            // Always renders through the sequencing engine — that's the point of having an offline path.
            var engine = new SequencedDrumPatternSampleProvider(pattern, new DrumKit(), tempo) { Swing = swing };
            engine.Transport.Play();

            long totalSamples = (long)OfflineRenderSeconds * engine.WaveFormat.SampleRate * engine.WaveFormat.Channels;
            var buffer = new float[engine.WaveFormat.SampleRate * engine.WaveFormat.Channels];
            try
            {
                using var writer = new WaveFileWriter(dialog.FileName, engine.WaveFormat);
                long written = 0;
                while (written < totalSamples)
                {
                    int toRead = (int)Math.Min(buffer.Length, totalSamples - written);
                    int read = engine.Read(buffer.AsSpan(0, toRead));
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
                if (legacyEngine != null) legacyEngine.Tempo = value;
                if (sequencingEngine != null) sequencingEngine.Tempo = value;
                OnPropertyChanged(nameof(Tempo));
            }
        }

        /// <summary>Swing amount, 0..0.5. Only applies to the sequencing engine.</summary>
        public double Swing
        {
            get => swing;
            set
            {
                if (swing == value) return;
                swing = value;
                if (sequencingEngine != null) sequencingEngine.Swing = value;
                OnPropertyChanged(nameof(Swing));
            }
        }

        /// <summary>When true, plays through the original PatternSequencer. When false (default), plays
        /// through the new NAudio.Sequencing engine. Toggle while stopped to A/B them.</summary>
        public bool UseLegacyEngine
        {
            get => useLegacyEngine;
            set
            {
                if (useLegacyEngine == value) return;
                useLegacyEngine = value;
                OnPropertyChanged(nameof(UseLegacyEngine));
            }
        }
    }
}
