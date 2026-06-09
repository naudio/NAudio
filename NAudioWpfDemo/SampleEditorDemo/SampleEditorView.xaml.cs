using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace NAudioWpfDemo.SampleEditorDemo
{
    /// <summary>
    /// Interaction logic for SampleEditorView.xaml. Wires the waveform's draggable
    /// markers and the on-screen keyboard to the view-model, refreshes the waveform
    /// whenever a new sample is loaded, and keeps the keyboard's root-key highlight
    /// in sync with the root-key slider.
    /// </summary>
    public partial class SampleEditorView : UserControl
    {
        private SampleEditorViewModel wired;

        public SampleEditorView()
        {
            InitializeComponent();
            waveform.MarkerMoved += (marker, index) => wired?.SetMarkerFromWaveform(marker, index);
            piano.NoteOn += (note, velocity) => wired?.PlayNote(note, velocity);
            piano.NoteOff += note => wired?.StopNote(note);
            DataContextChanged += OnDataContextChanged;
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
}
