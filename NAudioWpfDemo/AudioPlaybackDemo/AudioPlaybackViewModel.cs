using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.AudioPlaybackDemo
{
    class AudioPlaybackViewModel : ViewModelBase, IDisposable
    {
        private readonly AudioPlayback audioPlayback;
        private readonly List<IVisualizationPlugin> visualizations;
        private IVisualizationPlugin selectedVisualization;
        private string selectedFile;

        public ICommand OpenFileCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        public AudioPlaybackViewModel(IEnumerable<IVisualizationPlugin> visualizations)
        {
            this.visualizations = new List<IVisualizationPlugin>(visualizations);
            this.selectedVisualization = this.visualizations.FirstOrDefault();

            this.audioPlayback = new AudioPlayback();
            audioPlayback.MaximumCalculated += audioGraph_MaximumCalculated;
            audioPlayback.FftCalculated += audioGraph_FftCalculated;

            PlayCommand = new DelegateCommand(Play);
            OpenFileCommand = new DelegateCommand(OpenFile);
            StopCommand = new DelegateCommand(Stop);
            PauseCommand = new DelegateCommand(Pause);
        }

        private void Pause()
        {
            audioPlayback.Pause();
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
                    OnPropertyChanged("SelectedVisualization");
                    OnPropertyChanged("Visualization");
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

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                this.selectedFile = openFileDialog.FileName;
                audioPlayback.Load(this.selectedFile);
            }
        }

        private void Play()
        {
            if (this.selectedFile == null)
            {
                OpenFile();
            }
            if (this.selectedFile != null)
            {
                audioPlayback.Play();
            }
        }

        private void Stop()
        {
            audioPlayback.Stop();
        }

        public void Dispose()
        {
            audioPlayback.Dispose();
        }
    }
}
