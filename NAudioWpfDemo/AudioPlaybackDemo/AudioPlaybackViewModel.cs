using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows;
using NAudio.Wave;
using NAudioWpfDemo.AudioPlaybackDemo;

namespace NAudioWpfDemo
{
    class AudioPlaybackViewModel : INotifyPropertyChanged, IDisposable
    {
        private int captureSeconds;
        private AudioGraph audioGraph;
        private List<IVisualizationPlugin> visualizations;
        private IVisualizationPlugin selectedVisualization;

        public AudioPlaybackViewModel(IEnumerable<IVisualizationPlugin> visualizations)
        {
            this.visualizations = new List<IVisualizationPlugin>(visualizations);
            this.selectedVisualization = this.visualizations.FirstOrDefault();

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
            StopCommand = new RelayCommand(
                        () => this.Stop(),
                        () => true);
        }

        public IList<IVisualizationPlugin> Visualizations { get { return this.visualizations; } }

        public IVisualizationPlugin SelectedVisualization
        {
            get
            {
                return this.selectedVisualization;
            }
            set
            {
                if (this.selectedVisualization != value)
                {
                    this.selectedVisualization = value;
                    RaisePropertyChangedEvent("SelectedVisualization");
                    RaisePropertyChangedEvent("Visualization");
                }
            }
        }

        public object Visualization
        {
            get
            {
                return this.selectedVisualization.Content;
            }
        }

        void audioGraph_FftCalculated(object sender, FftEventArgs e)
        {
            if (this.SelectedVisualization != null)
            {
                this.SelectedVisualization.OnFftCalculated(e.Result);
            }
        }

        void audioGraph_MaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            if (this.SelectedVisualization != null)
            {
                this.SelectedVisualization.OnMaxCalculated(e.MinSample, e.MaxSample);
            }
        }

        void audioGraph_CaptureComplete(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public ICommand PlayFileCommand { get; private set; }
        public ICommand CaptureCommand { get; private set; }
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
