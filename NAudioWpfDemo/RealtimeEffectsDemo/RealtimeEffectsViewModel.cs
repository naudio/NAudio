using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Effects;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.RealtimeEffectsDemo
{
    class RealtimeEffectsViewModel : ViewModelBase, IDisposable
    {
        private readonly RealtimeAudioEngine engine = new RealtimeAudioEngine();
        private readonly DispatcherTimer timer;
        private string selectedDriver;
        private int inputChannelsIndex = 1;
        private int inputChannelOffset = 1;
        private int selectedEffectIndex;
        private bool monitoring;
        private double outputLevel;
        private string status = "Select an ASIO driver and press Start, or render a file.";
        private string inputFilePath;
        private IWavePlayer filePlayer;
        private AudioFileReader fileReader;

        public RealtimeEffectsViewModel()
        {
            Drivers = new ObservableCollection<string>(AsioDevice.GetDriverNames());
            if (Drivers.Count > 0)
                selectedDriver = Drivers[0];
            else
                status = "No ASIO drivers found (file render still works).";

            AvailableEffects = new ObservableCollection<string>();
            foreach (var entry in EffectCatalog.Entries)
                AvailableEffects.Add(entry.Name);

            Chain = new ObservableCollection<EffectSlotViewModel>();

            StartCommand = new DelegateCommand(Start) { IsEnabled = Drivers.Count > 0 };
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            AddEffectCommand = new DelegateCommand(AddEffect);
            ChooseInputFileCommand = new DelegateCommand(ChooseInputFile);
            RenderCommand = new DelegateCommand(RenderToFile) { IsEnabled = false };
            PlayFileCommand = new DelegateCommand(PlayFile) { IsEnabled = false };
            StopFileCommand = new DelegateCommand(StopFile) { IsEnabled = false };

            timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            timer.Tick += OnTick;
            timer.Start();
        }

        public ObservableCollection<string> Drivers { get; }

        public ObservableCollection<string> AvailableEffects { get; }

        public ObservableCollection<EffectSlotViewModel> Chain { get; }

        public string SelectedDriver
        {
            get => selectedDriver;
            set { selectedDriver = value; OnPropertyChanged(nameof(SelectedDriver)); }
        }

        public int InputChannelsIndex
        {
            get => inputChannelsIndex;
            set { inputChannelsIndex = value; OnPropertyChanged(nameof(InputChannelsIndex)); }
        }

        /// <summary>First ASIO input channel to use (1-based; e.g. 2 for a guitar
        /// on input 2, or 5 for a stereo pair on 5+6).</summary>
        public int InputChannelOffset
        {
            get => inputChannelOffset;
            set
            {
                inputChannelOffset = value < 1 ? 1 : value;
                OnPropertyChanged(nameof(InputChannelOffset));
            }
        }

        public int SelectedEffectIndex
        {
            get => selectedEffectIndex;
            set { selectedEffectIndex = value; OnPropertyChanged(nameof(SelectedEffectIndex)); }
        }

        public bool Monitoring
        {
            get => monitoring;
            set
            {
                monitoring = value;
                engine.Muted = !value;
                OnPropertyChanged(nameof(Monitoring));
            }
        }

        public double OutputLevel
        {
            get => outputLevel;
            private set { outputLevel = value; OnPropertyChanged(nameof(OutputLevel)); }
        }

        public string Status
        {
            get => status;
            private set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        public string InputFilePath
        {
            get => inputFilePath;
            private set { inputFilePath = value; OnPropertyChanged(nameof(InputFilePath)); }
        }

        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand AddEffectCommand { get; }
        public DelegateCommand ChooseInputFileCommand { get; }
        public DelegateCommand RenderCommand { get; }
        public DelegateCommand PlayFileCommand { get; }
        public DelegateCommand StopFileCommand { get; }

        private void AddEffect()
        {
            if (selectedEffectIndex < 0 || selectedEffectIndex >= EffectCatalog.Entries.Count)
                return;
            var entry = EffectCatalog.Entries[selectedEffectIndex];
            var slot = new EffectSlotViewModel(entry.Name, entry.Create(), Remove, MoveUp, MoveDown);
            Chain.Add(slot);
            RebuildLiveChain();
        }

        private void Remove(EffectSlotViewModel slot)
        {
            Chain.Remove(slot);
            RebuildLiveChain();
        }

        private void MoveUp(EffectSlotViewModel slot)
        {
            var i = Chain.IndexOf(slot);
            if (i > 0)
            {
                Chain.Move(i, i - 1);
                RebuildLiveChain();
            }
        }

        private void MoveDown(EffectSlotViewModel slot)
        {
            var i = Chain.IndexOf(slot);
            if (i >= 0 && i < Chain.Count - 1)
            {
                Chain.Move(i, i + 1);
                RebuildLiveChain();
            }
        }

        private void RebuildLiveChain()
        {
            var sampleRate = engine.IsRunning ? engine.SampleRate : 48000;
            var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
            var array = new IAudioEffect[Chain.Count];
            for (var i = 0; i < Chain.Count; i++)
            {
                array[i] = Chain[i].Effect;
                array[i].Configure(format);
            }
            engine.SetEffects(array);
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(selectedDriver))
                return;
            try
            {
                engine.Start(selectedDriver, inputChannelsIndex == 0 ? 1 : 2, inputChannelOffset - 1);
                RebuildLiveChain();
                Monitoring = false;
                Status = $"Running on '{selectedDriver}' at {engine.SampleRate} Hz. " +
                         "Output is muted — enable monitoring to listen.";
                StartCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                // The chain is shared; don't let the file path drive it on a
                // second audio thread while ASIO owns it.
                PlayFileCommand.IsEnabled = false;
                RenderCommand.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Status = "Failed to start: " + ex.Message;
            }
        }

        private void Stop()
        {
            engine.Stop();
            Monitoring = false;
            OutputLevel = 0;
            StartCommand.IsEnabled = Drivers.Count > 0;
            StopCommand.IsEnabled = false;
            var hasFile = !string.IsNullOrEmpty(inputFilePath);
            PlayFileCommand.IsEnabled = hasFile;
            RenderCommand.IsEnabled = hasFile;
            Status = "Stopped.";
        }

        private void ChooseInputFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                InputFilePath = dialog.FileName;
                RenderCommand.IsEnabled = true;
                PlayFileCommand.IsEnabled = true;
                Status = "Input file selected.";
            }
        }

        private void PlayFile()
        {
            if (engine.IsRunning)
            {
                Status = "Stop ASIO monitoring before playing a file through the chain.";
                return;
            }
            StopFile();
            try
            {
                fileReader = new AudioFileReader(inputFilePath);
                var chain = new EffectChain(fileReader);
                foreach (var slot in Chain)
                    chain.Add(slot.Effect);
                filePlayer = new WaveOut();
                filePlayer.Init(chain);
                filePlayer.Play();
                PlayFileCommand.IsEnabled = false;
                StopFileCommand.IsEnabled = true;
                StartCommand.IsEnabled = false;
                Status = "Playing file through the chain.";
            }
            catch (Exception ex)
            {
                Status = "Playback failed: " + ex.Message;
            }
        }

        private void StopFile()
        {
            filePlayer?.Dispose();
            filePlayer = null;
            fileReader?.Dispose();
            fileReader = null;
            PlayFileCommand.IsEnabled = !string.IsNullOrEmpty(inputFilePath);
            StopFileCommand.IsEnabled = false;
            StartCommand.IsEnabled = Drivers.Count > 0 && !engine.IsRunning;
        }

        private void RenderToFile()
        {
            if (engine.IsRunning)
            {
                Status = "Stop ASIO monitoring before rendering through the chain.";
                return;
            }
            var dialog = new SaveFileDialog { Filter = "WAV file (*.wav)|*.wav" };
            if (dialog.ShowDialog() != true)
                return;
            try
            {
                using var reader = new AudioFileReader(inputFilePath);
                var chain = new EffectChain(reader);
                foreach (var slot in Chain)
                    chain.Add(slot.Effect);

                using var writer = new WaveFileWriter(dialog.FileName, reader.WaveFormat);
                var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int read;
                while ((read = chain.Read(buffer)) > 0)
                    writer.WriteSamples(buffer, 0, read);

                Status = "Rendered to " + dialog.FileName;
            }
            catch (Exception ex)
            {
                Status = "Render failed: " + ex.Message;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            OutputLevel = engine.OutputLevel;
            foreach (var slot in Chain)
                slot.RefreshMeters();
            if (engine.ConsumeAutoMuted())
            {
                monitoring = false;
                OnPropertyChanged(nameof(Monitoring));
                Status = "Feedback detected — output auto-muted. Use headphones, then re-enable monitoring.";
            }
        }

        public void Dispose()
        {
            timer.Stop();
            StopFile();
            engine.Dispose();
        }
    }
}
