using System;
using System.Linq;
using Windows.Storage.Streams;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Win8.Wave.WaveOutputs;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using System.IO;
using NAudio.MediaFoundation;

namespace NAudioUniversalDemo
{
    class MainPageViewModel : ViewModelBase
    {
        private IWavePlayer player;
        private WaveStream reader;
        private IWaveIn recorder;
        private MemoryStream recordStream;
        private IRandomAccessStream selectedStream;

        public MainPageViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play) { IsEnabled = false };
            PauseCommand = new DelegateCommand(Pause) { IsEnabled = false };
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            RecordCommand = new DelegateCommand(Record);
            StopRecordingCommand = new DelegateCommand(StopRecording) { IsEnabled = false };
            MediaFoundationApi.Startup();
        }
        
        private void Stop()
        {
            player?.Stop();
        }

        private void Pause()
        {
            player?.Pause();
        }

        private void Play()
        {
            if (player == null)
            {
                // Exclusive mode - fails with a weird buffer alignment error
                player = new WasapiOutRT(AudioClientShareMode.Shared, 200);
                player.Init(CreateReader);

                player.PlaybackStopped += PlayerOnPlaybackStopped;
            }

            if (player.PlaybackState != PlaybackState.Playing)
            {
                //reader.Seek(0, SeekOrigin.Begin);
                player.Play();
                StopCommand.IsEnabled = true;
                PauseCommand.IsEnabled = true;
                LoadCommand.IsEnabled = false;
            }
        }

        private IWaveProvider CreateReader()
        {
            if (reader is RawSourceWaveStream)
            {
                reader.Position = 0;
                return reader;
            }
            reader = new MediaFoundationReaderUniversal(selectedStream);
            return reader;
        }        

        private void Record()
        {
            if (recorder == null)
            {
                recorder = new WasapiCaptureRT();
                recorder.RecordingStopped += RecorderOnRecordingStopped;
                recorder.DataAvailable += RecorderOnDataAvailable;               
            }

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            
            recorder.StartRecording();

            RecordCommand.IsEnabled = false;
            StopRecordingCommand.IsEnabled = true;
        }   

       

        private async void RecorderOnDataAvailable(object sender, WaveInEventArgs waveInEventArgs)
        {
            if (reader == null)
            {
                recordStream = new MemoryStream();
                reader = new RawSourceWaveStream(recordStream, recorder.WaveFormat);                
            }      
     
            await recordStream.WriteAsync(waveInEventArgs.Buffer, 0, waveInEventArgs.BytesRecorded);                      
        }

        private void StopRecording()
        {
            recorder?.StopRecording();
        }

        private void RecorderOnRecordingStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            RecordCommand.IsEnabled = true;
            StopRecordingCommand.IsEnabled = false;            
            PlayCommand.IsEnabled = true;    
        }


        private void PlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            LoadCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
            PauseCommand.IsEnabled = false;
            if (reader != null)
            {
                reader.Position = 0;
            }
        }

        private async void Load()
        {
            if (player != null)
            {
                player.Dispose();
                player = null;
            }
            reader = null; // will be disposed by player

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            var stream = await file.OpenAsync(FileAccessMode.Read);
            if (stream == null) return;
            selectedStream = stream; 
            PlayCommand.IsEnabled = true;
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand PlayCommand { get; }
        public DelegateCommand PauseCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand RecordCommand { get; }
        public DelegateCommand StopRecordingCommand { get; }

        public MediaElement MediaElement { get; set; }
    }


}
