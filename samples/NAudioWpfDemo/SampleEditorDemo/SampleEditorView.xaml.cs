using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NAudioWpfDemo.SampleEditorDemo;

/// <summary>
/// Interaction logic for SampleEditorView.xaml. Wires the waveform's draggable
/// markers and the on-screen keyboard to the view-model, refreshes the waveform
/// whenever a new sample is loaded, keeps the keyboard's root-key highlight in
/// sync, and drives a playback-position indicator on the waveform from a UI-rate
/// timer (reading the sounding voices' read positions).
/// </summary>
public partial class SampleEditorView : UserControl
{
    private SampleEditorViewModel wired;
    private readonly DispatcherTimer playheadTimer;
    private readonly double[] playheads = new double[64]; // ample for the voice pool

    public SampleEditorView()
    {
        InitializeComponent();
        waveform.MarkerMoved += (marker, index) => wired?.SetMarkerFromWaveform(marker, index);
        piano.NoteOn += (note, velocity) => wired?.PlayNote(note, velocity);
        piano.NoteOff += note => wired?.StopNote(note);
        DataContextChanged += OnDataContextChanged;

        // ~30 fps: cheap (reads a few doubles, moves a line per voice) and only
        // runs while the panel is loaded
        playheadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        playheadTimer.Tick += (_, _) => UpdatePlayheads();
        Loaded += (_, _) => playheadTimer.Start();
        Unloaded += (_, _) => playheadTimer.Stop();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (wired != null)
        {
            wired.SampleLoaded -= RefreshWaveform;
            wired.PropertyChanged -= OnViewModelPropertyChanged;
        }
        wired = DataContext as SampleEditorViewModel;
        if (wired != null)
        {
            wired.SampleLoaded += RefreshWaveform;
            wired.PropertyChanged += OnViewModelPropertyChanged;
            piano.RootKey = wired.RootKey;
        }
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SampleEditorViewModel.RootKey))
            piano.RootKey = wired.RootKey;
    }

    private void UpdatePlayheads()
    {
        int count = wired?.ReadPlaybackPositions(playheads) ?? 0;
        waveform.SetPlayheads(playheads, count);
    }

    private void RefreshWaveform()
    {
        var samples = wired.WaveformSamples;
        waveform.SetSample(samples, samples?.Length ?? 0);
        waveform.SetMarker(SampleMarker.Start, wired.StartIndex);
        waveform.SetMarker(SampleMarker.End, wired.EndIndex);
        waveform.SetMarker(SampleMarker.LoopStart, wired.LoopStartIndex);
        waveform.SetMarker(SampleMarker.LoopEnd, wired.LoopEndIndex);
        piano.RootKey = wired.RootKey;
    }
}
