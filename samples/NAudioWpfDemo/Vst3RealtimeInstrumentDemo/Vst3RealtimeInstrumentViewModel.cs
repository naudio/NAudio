using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using NAudio.Midi;
using NAudio.Vst3;
using NAudioWpfDemo.ViewModel;
using NAudioWpfDemo.Vst3Shared;
using Windows.Devices.Enumeration;

namespace NAudioWpfDemo.Vst3RealtimeInstrumentDemo
{
    /// <summary>
    /// Drives the live-VSTi WPF demo: pick an instrument from the shared catalogue (filtered to
    /// instruments), pick a WinRT MIDI input device, choose an ASIO driver, press Start to play
    /// the keyboard through the synth and out the speakers. Editor pops out via the shared
    /// <see cref="Vst3EditorWindow"/>. Master volume and a Panic button (all-notes-off) cover
    /// the live-comfort essentials.
    /// </summary>
    class Vst3RealtimeInstrumentViewModel : ViewModelBase, IDisposable
    {
        private readonly Vst3RealtimeInstrumentEngine engine = new Vst3RealtimeInstrumentEngine();
        private readonly DispatcherTimer timer;

        private string selectedDriver;
        private Vst3InstalledPlugin selectedInstrument;
        private MidiInputItem selectedMidi;
        private double outputLevel;
        private double masterVolumeDb;
        private string status;
        private bool disposed;
        private bool isStopped = true;
        private Vst3EditorWindow editorWindow;

        public Vst3RealtimeInstrumentViewModel()
        {
            Drivers = new ObservableCollection<string>(SafeListDrivers());
            if (Drivers.Count > 0)
            {
                var remembered = DemoSettings.LastAsioDriver;
                selectedDriver = !string.IsNullOrEmpty(remembered) && Drivers.Contains(remembered)
                    ? remembered : Drivers[0];
            }

            Instruments = new ObservableCollection<Vst3InstalledPlugin>();
            MidiInputs = new ObservableCollection<MidiInputItem>();

            StartCommand = new DelegateCommand(Start) { IsEnabled = false };
            StopCommand = new DelegateCommand(Stop) { IsEnabled = false };
            PanicCommand = new DelegateCommand(() => engine.Panic()) { IsEnabled = false };
            ShowEditorCommand = new DelegateCommand(ShowEditor) { IsEnabled = false };
            RescanInstrumentsCommand = new DelegateCommand(RescanInstruments) { IsEnabled = false };
            RefreshMidiCommand = new DelegateCommand(RefreshMidi);

            timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(50),
            };
            timer.Tick += OnTick;
            timer.Start();

            status = "Loading installed VST 3 instruments…";
            _ = LoadInstrumentsAsync(Vst3InstalledPlugins.GetAsync());
            _ = LoadMidiDevicesAsync();
        }

        public ObservableCollection<string> Drivers { get; }
        public ObservableCollection<Vst3InstalledPlugin> Instruments { get; }
        public ObservableCollection<MidiInputItem> MidiInputs { get; }

        public string SelectedDriver
        {
            get => selectedDriver;
            set
            {
                selectedDriver = value;
                if (!string.IsNullOrEmpty(value)) DemoSettings.LastAsioDriver = value;
                OnPropertyChanged(nameof(SelectedDriver));
                UpdateStartEnabled();
            }
        }

        public Vst3InstalledPlugin SelectedInstrument
        {
            get => selectedInstrument;
            set
            {
                selectedInstrument = value;
                OnPropertyChanged(nameof(SelectedInstrument));
                UpdateStartEnabled();
            }
        }

        public MidiInputItem SelectedMidi
        {
            get => selectedMidi;
            set
            {
                selectedMidi = value;
                if (value != null) DemoSettings.LastMidiDeviceId = value.Id;
                OnPropertyChanged(nameof(SelectedMidi));
                UpdateStartEnabled();
            }
        }

        public double OutputLevel
        {
            get => outputLevel;
            private set { outputLevel = value; OnPropertyChanged(nameof(OutputLevel)); }
        }

        /// <summary>Master output volume in dB. 0 dB = unity; applied post-instrument.</summary>
        public double MasterVolumeDb
        {
            get => masterVolumeDb;
            set
            {
                masterVolumeDb = value;
                engine.MasterGain = (float)Math.Pow(10.0, value / 20.0);
                OnPropertyChanged(nameof(MasterVolumeDb));
            }
        }

        public string Status
        {
            get => status;
            private set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        /// <summary>
        /// True while the engine is stopped. Bound to the IsEnabled of the ASIO / MIDI / instrument
        /// dropdowns so the user can't pick a different device or plug-in mid-playback — the engine
        /// can't swap any of them live, so changing the selection would mislead them.
        /// </summary>
        public bool IsStopped
        {
            get => isStopped;
            private set { isStopped = value; OnPropertyChanged(nameof(IsStopped)); }
        }

        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }
        public DelegateCommand PanicCommand { get; }
        public DelegateCommand ShowEditorCommand { get; }
        public DelegateCommand RescanInstrumentsCommand { get; }
        public DelegateCommand RefreshMidiCommand { get; }

        private void UpdateStartEnabled()
        {
            StartCommand.IsEnabled = !engine.IsRunning
                && !string.IsNullOrEmpty(selectedDriver)
                && selectedInstrument != null
                && selectedMidi != null;
        }

        private async void Start()
        {
            try
            {
                engine.Start(selectedDriver, selectedInstrument.Module, selectedInstrument.Class,
                    (float)Math.Pow(10.0, masterVolumeDb / 20.0));
                await engine.ConnectMidiAsync(selectedMidi.Id);
                Status = $"Live — playing {selectedInstrument.Display} via {selectedDriver} " +
                         $"({engine.SampleRate} Hz, {engine.FramesPerBuffer} frames/buffer). " +
                         "MIDI in: " + selectedMidi.Name + ".";
                StartCommand.IsEnabled = false;
                StopCommand.IsEnabled = true;
                PanicCommand.IsEnabled = true;
                ShowEditorCommand.IsEnabled = engine.View != null;
                IsStopped = false;
            }
            catch (Exception ex)
            {
                Status = "Start failed: " + ex.Message;
                CleanupAfterFailedStart();
            }
        }

        private void Stop()
        {
            CloseEditorWindow();
            engine.Stop();
            StopCommand.IsEnabled = false;
            PanicCommand.IsEnabled = false;
            ShowEditorCommand.IsEnabled = false;
            IsStopped = true;
            UpdateStartEnabled();
            OutputLevel = 0;
            Status = $"Stopped. {engine.NotesPlayed} note-ons received.";
        }

        private void CleanupAfterFailedStart()
        {
            try { engine.Stop(); } catch { /* ignore */ }
            StopCommand.IsEnabled = false;
            PanicCommand.IsEnabled = false;
            ShowEditorCommand.IsEnabled = false;
            IsStopped = true;
            UpdateStartEnabled();
        }

        private void ShowEditor()
        {
            if (engine.View == null) return;
            if (editorWindow != null)
            {
                editorWindow.Activate();
                return;
            }
            editorWindow = new Vst3EditorWindow(selectedInstrument?.Display ?? "VST 3 Instrument", engine.View);
            editorWindow.ClosedByUser += (_, _) => editorWindow = null;
            editorWindow.Show();
        }

        private void CloseEditorWindow()
        {
            if (editorWindow == null) return;
            var w = editorWindow;
            editorWindow = null;
            try { w.Close(); } catch { /* already closing */ }
        }

        private void OnTick(object sender, EventArgs e)
        {
            OutputLevel = engine.OutputLevel;
        }

        private void RescanInstruments()
        {
            Instruments.Clear();
            SelectedInstrument = null;
            RescanInstrumentsCommand.IsEnabled = false;
            Status = "Rescanning installed VST 3 plug-ins…";
            _ = LoadInstrumentsAsync(Vst3InstalledPlugins.RescanAsync());
        }

        private async Task LoadInstrumentsAsync(Task<IReadOnlyList<Vst3InstalledPlugin>> source)
        {
            IReadOnlyList<Vst3InstalledPlugin> instruments;
            try
            {
                var all = await source.ConfigureAwait(true);
                instruments = all.Where(p => p.Class.IsInstrument).ToList();
            }
            catch (Exception ex)
            {
                if (disposed) return;
                Status = "Plug-in scan failed: " + ex.Message;
                RescanInstrumentsCommand.IsEnabled = true;
                return;
            }
            if (disposed) return;
            foreach (var i in instruments) Instruments.Add(i);
            RescanInstrumentsCommand.IsEnabled = true;
            Status = instruments.Count == 0
                ? "No VST 3 instruments found in the standard install folders."
                : Drivers.Count > 0
                    ? "Pick an instrument, a MIDI input, and an ASIO driver, then press Start."
                    : "No ASIO drivers found — connect an audio interface and reopen this demo.";
            UpdateStartEnabled();
        }

        private async void RefreshMidi()
        {
            MidiInputs.Clear();
            SelectedMidi = null;
            await LoadMidiDevicesAsync();
        }

        private async Task LoadMidiDevicesAsync()
        {
            IReadOnlyList<DeviceInformation> devices;
            try
            {
                devices = await WinRTMidiIn.GetDevicesAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (disposed) return;
                Status = "MIDI enumeration failed: " + ex.Message;
                return;
            }
            if (disposed) return;
            foreach (var d in devices) MidiInputs.Add(new MidiInputItem(d.Id, d.Name));
            if (MidiInputs.Count > 0 && SelectedMidi == null)
            {
                var rememberedId = DemoSettings.LastMidiDeviceId;
                var remembered = !string.IsNullOrEmpty(rememberedId)
                    ? MidiInputs.FirstOrDefault(d => d.Id == rememberedId) : null;
                SelectedMidi = remembered ?? MidiInputs[0];
            }
            if (MidiInputs.Count == 0)
                Status = "No MIDI input devices found — connect a keyboard and click Refresh.";
        }

        private static IEnumerable<string> SafeListDrivers()
        {
            try { return NAudio.Wave.AsioDevice.GetDriverNames(); }
            catch { return Array.Empty<string>(); }
        }

        public void Dispose()
        {
            disposed = true;
            timer.Stop();
            CloseEditorWindow();
            engine.Dispose();
        }
    }

    /// <summary>One WinRT MIDI input device, surfaced to the picker.</summary>
    sealed class MidiInputItem
    {
        public MidiInputItem(string id, string name) { Id = id; Name = name; }
        public string Id { get; }
        public string Name { get; }
        public override string ToString() => Name;
    }
}
