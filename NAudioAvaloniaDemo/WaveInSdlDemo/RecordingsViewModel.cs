using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudioAvaloniaDemo.Utils;
using NAudioAvaloniaDemo.ViewModel;

namespace NAudioAvaloniaDemo.WaveInSdlDemo
{
    class RecordingsViewModel : ViewModelBase
    {
        public string OutputFolder { get; }
        private string selectedRecording;
        public ObservableCollection<string> Recordings { get; }
        public DelegateCommand PlayCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand OpenFolderCommand { get; }

        public RecordingsViewModel()
        {
            Recordings = new ObservableCollection<string>();
            OutputFolder = Path.Combine(Path.GetTempPath(), "NAudioAvaloniaDemo");
            Directory.CreateDirectory(OutputFolder);
            foreach (var file in Directory.GetFiles(OutputFolder))
            {
                Recordings.Add(file);
            }
            PlayCommand = new DelegateCommand(Play);
            DeleteCommand = new DelegateCommand(Delete);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            EnableCommands();
        }

        private void OpenFolder()
        {
            ShellExecute(OutputFolder);
        }

        private static void ShellExecute(string file)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo(file)
            {
                UseShellExecute = true
            };
            process.Start();
        }

        private async void Delete()
        {
            if (SelectedRecording != null)
            {
                try
                {
                    File.Delete(Path.Combine(OutputFolder, SelectedRecording));
                    Recordings.Remove(SelectedRecording);
                    SelectedRecording = Recordings.FirstOrDefault();
                }
                catch (Exception)
                {
                    await MessageBox.ShowAsync("Could not delete recording");
                }
            }
        }

        private void Play()
        {
            if (SelectedRecording != null)
            {
                ShellExecute(Path.Combine(OutputFolder, SelectedRecording));
            }
        }

        public string SelectedRecording
        {
            get => selectedRecording;
            set
            {
                SetProperty(ref selectedRecording, value);
                EnableCommands();
            }
        }

        private void EnableCommands()
        {
            PlayCommand.IsEnabled = SelectedRecording != null;
            DeleteCommand.IsEnabled = SelectedRecording != null;
        }
    }
}
