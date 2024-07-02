using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NAudio.Extras;
using NAudioAvaloniaDemo.ViewModel;

namespace NAudioAvaloniaDemo.AudioPlaybackDemo
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
            Dispatcher.UIThread.Invoke(() =>
            {
                if (this.SelectedVisualization != null)
                {
                    this.SelectedVisualization.OnFftCalculated(e.Result);
                }
            });
        }

        void audioGraph_MaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (this.SelectedVisualization != null)
                {
                    this.SelectedVisualization.OnMaxCalculated(e.MinSample, e.MaxSample);
                }
            });
        }

        private async void OpenFile()
        {
            var openOptions = new FilePickerOpenOptions()
            {
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new FilePickerFileType("All Supported Files (*.wav)")
                    {
                        Patterns = new[] { "*.wav", }
                    },
                    new FilePickerFileType("All Files (*.*)")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            };
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(openOptions);
            if (files.Count == 0) return;
            var selectedFilePath = files[0].Path.LocalPath;

            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                this.selectedFile = selectedFilePath;
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
