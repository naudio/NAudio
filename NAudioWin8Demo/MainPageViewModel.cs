using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NAudio.MediaFoundation;
using Windows.Storage.Pickers;

namespace NAudioWin8Demo
{
    class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            PlayCommand = new DelegateCommand(Play);
            PauseCommand = new DelegateCommand(Pause);
            StopCommand = new DelegateCommand(Stop);
        }

        private void Stop()
        {
            throw new System.NotImplementedException();
        }

        private void Pause()
        {
            throw new System.NotImplementedException();
        }

        private void Play()
        {
            throw new System.NotImplementedException();
        }

        private async void Load()
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            var file = await picker.PickSingleFileAsync();
            using (var stream = await file.OpenReadAsync())
            {
                var byteStream = MediaFoundationApi.CreateByteStream(stream);
                var reader = MediaFoundationApi.CreateSourceReaderFromByteStream(byteStream);
                // TODO: move some of this interop into MediaFoundationReader
            }
        }

        public ICommand LoadCommand { get; private set; }
        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

    }
}
