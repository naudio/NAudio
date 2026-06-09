using System.Windows;
using System.Windows.Controls;

namespace NAudioWpfDemo.SampleEditorDemo
{
    /// <summary>
    /// Interaction logic for SampleEditorView.xaml. Wires the waveform's draggable
    /// markers and the on-screen keyboard to the view-model, and refreshes the
    /// waveform whenever a new sample is loaded.
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
            if (wired != null) wired.SampleLoaded -= RefreshWaveform;
            wired = DataContext as SampleEditorViewModel;
            if (wired != null) wired.SampleLoaded += RefreshWaveform;
        }

        private void RefreshWaveform()
        {
            var samples = wired.WaveformSamples;
            waveform.SetSample(samples, samples?.Length ?? 0);
            waveform.SetMarker(SampleMarker.Start, wired.StartIndex);
            waveform.SetMarker(SampleMarker.End, wired.EndIndex);
            waveform.SetMarker(SampleMarker.LoopStart, wired.LoopStartIndex);
            waveform.SetMarker(SampleMarker.LoopEnd, wired.LoopEndIndex);
        }
    }
}
