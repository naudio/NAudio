using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Win8.Wave.WaveOutputs;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace NAudioWin8Demo
{
    class MainPageViewModel : ViewModelBase
    {
        private IWavePlayer player;
        private WaveStream reader;

        public MainPageViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play) { IsEnabled = false };
            PauseCommand = new DelegateCommand(Pause) { IsEnabled = false };
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
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
                // current problems - Shared mode - resampler is not supported
                // Exclusive mode - fails with a weird buffer alignment error
                player = new WasapiOutRT(AudioClientShareMode.Shared, 100);
                player.PlaybackStopped += PlayerOnPlaybackStopped;
                await player.Init(reader);
            }
            player.Play();
            StopCommand.IsEnabled = true;
            PauseCommand.IsEnabled = true;
        }

        private void PlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            LoadCommand.IsEnabled = true;
            StopCommand.IsEnabled = false;
            PauseCommand.IsEnabled = false;
        }

        private async void Load()
        {
            var mfts = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect).ToList();

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            using (var stream = await file.OpenReadAsync())
            {
                reader = new MediaFoundationReaderRT(stream);
                PlayCommand.IsEnabled = true;
            }
        }

        public DelegateCommand LoadCommand { get; private set; }
        public DelegateCommand PlayCommand { get; private set; }
        public DelegateCommand PauseCommand { get; private set; }
        public DelegateCommand StopCommand { get; private set; }

    }

    // Slightly hacky approach to supporting a different WinRT constructor
    class MediaFoundationReaderRT : MediaFoundationReader
    {
        public class MediaFoundationReaderRTSettings : MediaFoundationReaderSettings
        {
            public IRandomAccessStream Stream { get; set; }
        }

        public MediaFoundationReaderRT(IRandomAccessStream stream)
            : this(new MediaFoundationReaderRTSettings() {Stream = stream})
        {
            
        }
        

        public MediaFoundationReaderRT(MediaFoundationReaderRTSettings settings)
            : base(null, settings)
        {
            
        }

        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var byteStream = MediaFoundationApi.CreateByteStream(((MediaFoundationReaderRTSettings)settings).Stream);
            var reader = MediaFoundationApi.CreateSourceReaderFromByteStream(byteStream);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, false);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);

            // Create a partial media type indicating that we want uncompressed PCM audio

            var partialMediaType = new MediaType();
            partialMediaType.MajorType = MediaTypes.MFMediaType_Audio;
            partialMediaType.SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM;

            // set the media type
            // can return MF_E_INVALIDMEDIATYPE if not supported
            reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
            return reader;
        }
    }
}
