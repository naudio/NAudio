using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.WasapiCaptureDemo
{
    class RecordingsViewModel : ViewModelBase
    {
        public string OutputFolder { get; private set; }
        private string selectedRecording;
        public ObservableCollection<string> Recordings { get; private set; }
        public DelegateCommand PlayCommand { get; private set; }
        public DelegateCommand DeleteCommand { get; private set; }
        public DelegateCommand OpenFolderCommand { get; private set; }

        public RecordingsViewModel()
        {
            Recordings = new ObservableCollection<string>();
            OutputFolder = Path.Combine(Path.GetTempPath(), "NAudioWpfDemo");
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
            Process.Start(OutputFolder);
        }

        private void Delete()
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
                    MessageBox.Show("Could not delete recording");
                }
            }
        }

        private void Play()
        {
            if (SelectedRecording != null)
            {
                Process.Start(Path.Combine(OutputFolder, SelectedRecording));
            }
        }

        public string SelectedRecording
        {
            get { return selectedRecording; }
            set
            {
                if (selectedRecording != value)
                {
                    selectedRecording = value;
                    OnPropertyChanged("SelectedRecording");
                    EnableCommands();
                }
            }
        }

        private void EnableCommands()
        {
            PlayCommand.IsEnabled = SelectedRecording != null;
            DeleteCommand.IsEnabled = SelectedRecording != null;
        }
    }
}
