using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows;
using NAudio.Wave;

namespace NAudioWpfDemo
{
    class ControlPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        int captureSeconds;
        AudioGraph audioGraph;
        IWaveFormRenderer waveFormRenderer;
        SpectrumAnalyser analyzer;

        public ControlPanelViewModel(IWaveFormRenderer waveFormRenderer, SpectrumAnalyser analyzer)
        {
            this.waveFormRenderer = waveFormRenderer;
            this.analyzer = analyzer;
            this.audioGraph = new AudioGraph();
            audioGraph.CaptureComplete += new EventHandler(audioGraph_CaptureComplete);
            audioGraph.MaximumCalculated += new EventHandler<MaxSampleEventArgs>(audioGraph_MaximumCalculated);
            audioGraph.FftCalculated += new EventHandler<FftEventArgs>(audioGraph_FftCalculated);
            this.captureSeconds = 10;
            this.NotificationsPerSecond = 100;

            PlayFileCommand = new RelayCommand(
                        () => this.PlayFile(),
                        () => true);
            CaptureCommand = new RelayCommand(
                        () => this.Capture(),
                        () => true);
            PlayCapturedAudioCommand = new RelayCommand(
                        () => this.PlayCapturedAudio(),
                        () => this.HasCapturedAudio());
            SaveCapturedAudioCommand = new RelayCommand(
                        () => this.SaveCapturedAudio(),
                        () => this.HasCapturedAudio());
            StopCommand = new RelayCommand(
                        () => this.Stop(),
                        () => true);
        }

        void audioGraph_FftCalculated(object sender, FftEventArgs e)
        {
            analyzer.Update(e.Result);
        }

        void audioGraph_MaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            waveFormRenderer.AddValue(e.MaxSample, e.MinSample);
        }

        void audioGraph_CaptureComplete(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public ICommand PlayFileCommand { get; private set; }
        public ICommand CaptureCommand { get; private set; }
        public ICommand PlayCapturedAudioCommand { get; private set; }
        public ICommand SaveCapturedAudioCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        private void PlayFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                string file = openFileDialog.FileName;
                audioGraph.PlayFile(file);
            }
        }

        private void Capture()
        {
            try
            {
                audioGraph.StartCapture(CaptureSeconds);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void PlayCapturedAudio()
        {
            audioGraph.PlayCapturedAudio();
        }

        private bool HasCapturedAudio()
        {
            return audioGraph.HasCapturedAudio;
        }

        private void SaveCapturedAudio()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".wav";
            saveFileDialog.Filter = "WAVE File (*.wav)|*.wav";
            bool? result = saveFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                audioGraph.SaveRecordedAudio(saveFileDialog.FileName);
            }
        }

        private void Stop()
        {
            audioGraph.Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public int CaptureSeconds
        {
            get
            {
                return captureSeconds;
            }
            set
            {
                if (captureSeconds != value)
                {
                    captureSeconds = value;
                    RaisePropertyChangedEvent("CaptureSeconds");
                }
            }
        }

        public double RecordVolume
        {
            get
            {
                return audioGraph.RecordVolume;
            }
            set
            {
                if (audioGraph.RecordVolume != value)
                {
                    audioGraph.RecordVolume = value;
                    RaisePropertyChangedEvent("RecordVolume");
                }
            }
        }

        public int NotificationsPerSecond
        {
            get
            {
                return audioGraph.NotificationsPerSecond;
            }
            set
            {
                if (NotificationsPerSecond != value)
                {
                    audioGraph.NotificationsPerSecond = value;
                    RaisePropertyChangedEvent("NotificationsPerSecond");
                }
            }
        }

        public void Dispose()
        {
            audioGraph.Dispose();
        }
    }
}
