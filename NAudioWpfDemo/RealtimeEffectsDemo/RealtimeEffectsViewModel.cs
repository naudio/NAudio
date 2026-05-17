using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using NAudio.Wave;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.RealtimeEffectsDemo
{
    class RealtimeEffectsViewModel : ViewModelBase, IDisposable
    {
        private readonly RealtimeAudioEngine engine = new RealtimeAudioEngine();
        private readonly DispatcherTimer timer;
        private string selectedDriver;
        private int inputChannelsIndex = 1; // 0 = mono, 1 = stereo
        private bool monitoring;
        private double outputLevel;
        private string status = "Select an ASIO driver and press Start.";

        public RealtimeEffectsViewModel()
        {
            Drivers = new ObservableCollection<string>(AsioDevice.GetDriverNames());
            if (Drivers.Count > 0)
                selectedDriver = Drivers[0];
            else
                status = "No ASIO drivers found. Install an ASIO driver (e.g. ASIO4ALL).";

            StartCommand = new DelegateCommand(Start) { IsEnabled = Drivers.Count > 0 };
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };

            timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            timer.Tick += OnTick;
        }

        public ObservableCollection<string> Drivers { get; }

        public string SelectedDriver
        {
            get => selectedDriver;
            set { selectedDriver = value; OnPropertyChanged(nameof(SelectedDriver)); }
        }

        /// <summary>0 = mono input (duplicated to stereo), 1 = stereo input.</summary>
        public int InputChannelsIndex
        {
            get => inputChannelsIndex;
            set { inputChannelsIndex = value; OnPropertyChanged(nameof(InputChannelsIndex)); }
        }

        /// <summary>When true, audio passes through; false keeps the output muted.</summary>
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

        public DelegateCommand StartCommand { get; }

        public DelegateCommand StopCommand { get; }

        private void Start()
        {
            if (string.IsNullOrEmpty(selectedDriver))
                return;
            try
            {
                engine.Start(selectedDriver, inputChannelsIndex == 0 ? 1 : 2);
                Monitoring = false; // always start muted
                Status = $"Running on '{selectedDriver}' at {engine.SampleRate} Hz. " +
                         "Output is muted — enable monitoring to listen.";
                StartCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                timer.Start();
            }
            catch (Exception ex)
            {
                Status = "Failed to start: " + ex.Message;
            }
        }

        private void Stop()
        {
            timer.Stop();
            engine.Stop();
            Monitoring = false;
            OutputLevel = 0;
            StartCommand.IsEnabled = Drivers.Count > 0;
            StopCommand.IsEnabled = false;
            Status = "Stopped.";
        }

        private void OnTick(object sender, EventArgs e)
        {
            OutputLevel = engine.OutputLevel;
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
            engine.Dispose();
        }
    }
}
