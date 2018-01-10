using System;
using System.Linq;
using System.Windows.Input;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.FireAndForgetPlayback
{
    class FireAndForgetPlaybackViewModel : ViewModelBase, IDisposable
    {
        public ICommand KickCommand { get; }
        public ICommand CrashCommand { get; }
        public ICommand SnareCommand { get; }

        private AudioPlaybackEngine engine;
        readonly CachedSound kick = new CachedSound("Samples\\kick-trimmed.wav");
        readonly CachedSound crash = new CachedSound("Samples\\crash-trimmed.wav");


        public FireAndForgetPlaybackViewModel()
        {
            engine = new AudioPlaybackEngine();
            KickCommand = new DelegateCommand(() => engine.PlaySound(kick));
            CrashCommand = new DelegateCommand(() => engine.PlaySound(crash));
            SnareCommand = new DelegateCommand(() => engine.PlaySound("Samples\\snare-trimmed.wav"));
        }

        public void Dispose()
        {
            engine?.Dispose();
            engine = null;
        }
    }
}
