using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Wave;
using NAudioWpfDemo.Utils;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.LiveSamplerDemo
{
    /// <summary>
    /// Plays a SoundFont (.sf2) or SFZ (.sfz) instrument live from a MIDI input
    /// device and/or the on-screen keyboard. Incoming events are fed to a
    /// <see cref="SamplerEngine"/> through a <see cref="LiveMidiInstrument"/> (which
    /// marshals them onto the audio thread) and rendered straight to the speakers
    /// via <see cref="WasapiPlayer"/>.
    /// </summary>
    class LiveSamplerDemoViewModel : ViewModelBase, IDisposable
    {
        private const int SampleRate = 44100;

        private IWavePlayer waveOut;
        private SamplerEngine sampler;
        private LiveMidiInstrument instrument;
        private IMidiInput midiIn;
        private readonly DispatcherTimer voiceTimer;

        private string instrumentPath;
        private string status = "Choose a SoundFont (.sf2) or SFZ (.sfz) instrument, optionally pick a MIDI input, then Start.";
        private bool isRunning;
        private double masterVolumeDb; // dB attenuation for the volume slider; 0 = unity
        private int activeVoiceCount;
        private MidiDeviceItem selectedDevice;

        /// <summary>A MIDI input device: its display name and the id used to open it.</summary>
        public sealed record MidiDeviceItem(string Name, string Id)
        {
            public override string ToString() => Name;
        }

        public ObservableCollection<MidiDeviceItem> MidiInputDevices { get; } = new();

        public ICommand BrowseInstrumentCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand PanicCommand { get; }

        /// <summary>Raised (on the UI thread) when a note starts, so the keyboard can light up.</summary>
        public event Action<int> NotePlayed;

        /// <summary>Raised (on the UI thread) when a note stops, so the keyboard can clear it.</summary>
        public event Action<int> NoteReleased;

        public LiveSamplerDemoViewModel()
        {
            BrowseInstrumentCommand = new DelegateCommand(BrowseInstrument);
            RefreshDevicesCommand = new DelegateCommand(() => _ = RefreshDevicesAsync());
            StartCommand = new DelegateCommand(() => Start());
            StopCommand = new DelegateCommand(Stop);
            PanicCommand = new DelegateCommand(Panic);

            voiceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            voiceTimer.Tick += (_, _) => ActiveVoiceCount = sampler?.ActiveVoiceCount ?? 0;

            _ = RefreshDevicesAsync();
        }

        public string InstrumentPath
        {
            get => instrumentPath;
            set { instrumentPath = value; OnPropertyChanged(nameof(InstrumentPath)); }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        public bool IsRunning
        {
            get => isRunning;
            set { isRunning = value; OnPropertyChanged(nameof(IsRunning)); OnPropertyChanged(nameof(IsStopped)); }
        }

        /// <summary>Convenience inverse of <see cref="IsRunning"/> for enabling pre-start controls.</summary>
        public bool IsStopped => !isRunning;

        public MidiDeviceItem SelectedDevice
        {
            get => selectedDevice;
            set { selectedDevice = value; OnPropertyChanged(nameof(SelectedDevice)); }
        }

        // perceived loudness is logarithmic, so the slider works in dB: equal
        // slider travel then means equal audible change. The bottom of the
        // range is treated as mute.
        private const double MinVolumeDb = -60;

        private static float GainFromDb(double db) =>
            db <= MinVolumeDb ? 0f : (float)Math.Pow(10.0, db / 20.0);

        /// <summary>Master volume in dB (-60 = mute .. 0 = unity), applied live.</summary>
        public double MasterVolumeDb
        {
            get => masterVolumeDb;
            set
            {
                masterVolumeDb = value;
                OnPropertyChanged(nameof(MasterVolumeDb));
                OnPropertyChanged(nameof(MasterVolumeText));
                if (sampler != null) sampler.MasterGain = GainFromDb(value);
            }
        }

        /// <summary>The volume readout ("-12.0 dB", or "Mute" at the bottom of the range).</summary>
        public string MasterVolumeText => masterVolumeDb <= MinVolumeDb ? "Mute" : $"{masterVolumeDb:F1} dB";

        public int ActiveVoiceCount
        {
            get => activeVoiceCount;
            set { activeVoiceCount = value; OnPropertyChanged(nameof(ActiveVoiceCount)); }
        }

        private void BrowseInstrument()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Sampler instruments (*.sf2;*.sfz)|*.sf2;*.sfz|SoundFont (*.sf2)|*.sf2|SFZ (*.sfz)|*.sfz|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true) InstrumentPath = dialog.FileName;
        }

        private async System.Threading.Tasks.Task RefreshDevicesAsync()
        {
            MidiInputDevices.Clear();
            try
            {
                var devices = await WinRTMidiIn.GetDevicesAsync();
                foreach (var device in devices)
                    MidiInputDevices.Add(new MidiDeviceItem(device.Name, device.Id));
                SelectedDevice = MidiInputDevices.Count > 0 ? MidiInputDevices[0] : null;
            }
            catch (Exception ex)
            {
                Status = $"Could not enumerate MIDI inputs: {ex.Message}";
            }
        }

        private async void Start()
        {
            if (isRunning) return;
            if (string.IsNullOrEmpty(instrumentPath) || !File.Exists(instrumentPath))
            {
                Status = "Please choose a SoundFont (.sf2) or SFZ (.sfz) instrument.";
                return;
            }

            try
            {
                sampler = CreateSampler(instrumentPath);
                sampler.MasterGain = GainFromDb(masterVolumeDb);
                instrument = new LiveMidiInstrument(sampler);

                // prefer the modern WASAPI player (auto-converts the sampler's
                // 44.1 kHz float stereo to the device mix format), falling back to
                // WaveOut if WASAPI can't initialise on this device
                waveOut = SamplerPlayback.Create(instrument, OnPlaybackError);
                waveOut.Play();

                if (selectedDevice != null)
                {
                    midiIn = await WinRTMidiIn.CreateAsync(selectedDevice.Id);
                    midiIn.MessageReceived += OnMidiMessageReceived;
                    midiIn.Start();
                }

                IsRunning = true;
                voiceTimer.Start();
                Status = selectedDevice != null
                    ? $"Playing {Path.GetFileName(instrumentPath)} from {selectedDevice.Name} (and the on-screen keyboard)."
                    : $"Playing {Path.GetFileName(instrumentPath)} from the on-screen keyboard.";
            }
            catch (Exception ex)
            {
                Stop();
                Status = $"Could not start: {ex.Message}";
                MessageBox.Show(ex.ToString(), "Sampler error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Stop()
        {
            voiceTimer.Stop();
            if (midiIn != null)
            {
                midiIn.MessageReceived -= OnMidiMessageReceived;
                midiIn.Stop();
                midiIn.Dispose();
                midiIn = null;
            }
            if (waveOut != null)
            {
                waveOut.Dispose();
                waveOut = null;
            }
            instrument = null;
            sampler = null;
            ActiveVoiceCount = 0;
            if (isRunning) Status = "Stopped.";
            IsRunning = false;
        }

        private void Panic()
        {
            // route through the queue so it lands on the audio thread like every
            // other event: all sound off (CC120) cuts even ringing release tails
            instrument?.Send(new ControlChangeEvent(0, 1, (MidiController)120, 0));
        }

        private void OnPlaybackError(Exception ex)
        {
            Stop();
            Status = $"Playback stopped: {ex.Message}";
            MessageBox.Show(ex.ToString(), "Playback error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>Plays a note from the on-screen keyboard (UI thread).</summary>
        public void PlayNote(int note, int velocity) => instrument?.NoteOn(0, note, velocity);

        /// <summary>Stops a note from the on-screen keyboard (UI thread).</summary>
        public void StopNote(int note) => instrument?.NoteOff(0, note);

        private void OnMidiMessageReceived(object sender, MidiInMessageEventArgs e)
        {
            var midiEvent = e.MidiEvent;
            if (midiEvent == null) return;

            instrument?.Send(midiEvent);

            // light the on-screen keyboard to mirror the hardware input. The WinRT
            // callback is on a thread-pool thread, so marshal to the UI thread.
            if (midiEvent is NoteOnEvent on && on.Velocity > 0)
                RaiseOnUi(() => NotePlayed?.Invoke(on.NoteNumber));
            else if (midiEvent is NoteEvent off &&
                     (off.CommandCode == MidiCommandCode.NoteOff || off is NoteOnEvent))
                RaiseOnUi(() => NoteReleased?.Invoke(off.NoteNumber));
        }

        private static void RaiseOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
        }

        private static SamplerEngine CreateSampler(string path) =>
            path.EndsWith(".sfz", StringComparison.OrdinalIgnoreCase)
                ? SfzSampler.FromFile(path, SampleRate)
                : new SoundFontSampler(new NAudio.SoundFont.SoundFont(path), SampleRate);

        public void Dispose() => Stop();
    }
}
