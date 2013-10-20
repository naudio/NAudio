using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NAudio.Win8.Wave.WaveOutputs;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using System.IO;
using NAudio.MediaFoundation;

namespace NAudioWin8Demo
{
    class MainPageViewModel : ViewModelBase
    {
        private IWavePlayer player;
        private WaveStream reader;
        private IWaveIn recorder;
        private MemoryStream recordStream;

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
            if (player != null)
            {
                player.Stop();
            }
        }

        private void Pause()
        {
            if (player != null)
            {
                player.Pause();
            }
        }

        private async void Play()
        {
            if (reader == null)
            {
                return;
            }

            if (player == null)
            {
                // Exclusive mode - fails with a weird buffer alignment error

                //player = new MediaElementOut(MediaElement);
                player = new WasapiOutRT(AudioClientShareMode.Shared, 200);

                player.PlaybackStopped += PlayerOnPlaybackStopped;
            }

            if (player.PlaybackState != PlaybackState.Playing)
            {
                reader.Seek(0, SeekOrigin.Begin);
                await player.Init(reader);
                player.Play();
                StopCommand.IsEnabled = true;
                PauseCommand.IsEnabled = true;
            }
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
            if (recorder != null)
            {
                recorder.StopRecording();
            }
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
        }

        private async void Load()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            var stream = await file.OpenAsync(FileAccessMode.Read);//  .OpenReadAsync();
            if (stream == null) return;
            using (stream)
            {
                // trying to get thre reader created on an MTA Thread
                await Task.Run(() => reader = new MediaFoundationReaderRT(stream));
                
                PlayCommand.IsEnabled = true;
            }
        }

        public DelegateCommand LoadCommand { get; private set; }
        public DelegateCommand PlayCommand { get; private set; }
        public DelegateCommand PauseCommand { get; private set; }
        public DelegateCommand StopCommand { get; private set; }
        public DelegateCommand RecordCommand { get; private set; }
        public DelegateCommand StopRecordingCommand { get; private set; }

        public MediaElement MediaElement { get; set; }
    }


}
