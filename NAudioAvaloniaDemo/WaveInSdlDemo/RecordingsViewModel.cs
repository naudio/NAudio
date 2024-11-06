using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
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

        private async void ShellExecute(string file)
        {
            try
            {
                if (OperatingSystem.IsAndroid())
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest("Open", new Microsoft.Maui.Storage.ReadOnlyFile(file)));
                    return;
                }

                var process = new Process();
                process.StartInfo = new ProcessStartInfo(file)
                {
                    UseShellExecute = true
                };
                process.Start();
            }
            catch (Exception e)
            {
                await MessageBox.ShowAsync(e.Message);
            }
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
