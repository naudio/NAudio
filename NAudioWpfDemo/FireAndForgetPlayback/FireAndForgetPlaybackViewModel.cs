using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using NAudio.Extras;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.FireAndForgetPlayback
{
    class FireAndForgetPlaybackViewModel : ViewModelBase, IDisposable
    {
        public ICommand KickCommand { get; }
        public ICommand CrashCommand { get; }
        public ICommand SnareCommand { get; }

        // Resolve relative to the executable so samples load regardless of working
        // directory (e.g. when running via `dotnet run` the CWD is the project dir).
        private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "Samples");

        private AudioPlaybackEngine engine;
        readonly CachedSound kick = new CachedSound(Path.Combine(SamplesDir, "kick-trimmed.wav"));
        readonly CachedSound crash = new CachedSound(Path.Combine(SamplesDir, "crash-trimmed.wav"));


        public FireAndForgetPlaybackViewModel()
        {
            engine = new AudioPlaybackEngine();
            KickCommand = new DelegateCommand(() => engine.PlaySound(kick));
            CrashCommand = new DelegateCommand(() => engine.PlaySound(crash));
            SnareCommand = new DelegateCommand(() => engine.PlaySound(Path.Combine(SamplesDir, "snare-trimmed.wav")));
        }

        public void Dispose()
        {
            engine?.Dispose();
            engine = null;
        }
    }
}
