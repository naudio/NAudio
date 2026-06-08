using System.Windows;
using System.Windows.Controls;

namespace NAudioWpfDemo.LiveSamplerDemo
{
    /// <summary>
    /// Interaction logic for LiveSamplerDemoView.xaml. Wires the on-screen keyboard
    /// to the view-model: key presses become notes, and notes the view-model reports
    /// (e.g. from hardware MIDI) light the matching keys.
    /// </summary>
    public partial class LiveSamplerDemoView : UserControl
    {
        private LiveSamplerDemoViewModel wired;

        public LiveSamplerDemoView()
        {
            InitializeComponent();
            piano.NoteOn += (note, velocity) => (DataContext as LiveSamplerDemoViewModel)?.PlayNote(note, velocity);
            piano.NoteOff += note => (DataContext as LiveSamplerDemoViewModel)?.StopNote(note);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (wired != null)
            {
                wired.NotePlayed -= OnNotePlayed;
                wired.NoteReleased -= OnNoteReleased;
            }
            wired = DataContext as LiveSamplerDemoViewModel;
            if (wired != null)
            {
                wired.NotePlayed += OnNotePlayed;
                wired.NoteReleased += OnNoteReleased;
            }
        }

        private void OnNotePlayed(int note) => piano.SetNoteState(note, true);

        private void OnNoteReleased(int note) => piano.SetNoteState(note, false);
    }
}
