using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAudioWpfDemo.LiveSamplerDemo
{
    /// <summary>
    /// A clickable on-screen piano keyboard. Raises <see cref="NoteOn"/> /
    /// <see cref="NoteOff"/> as the user presses and drags across keys, and lets an
    /// external source (e.g. a hardware MIDI input) light keys via
    /// <see cref="SetNoteState"/>. Notes use standard MIDI numbering (middle C = 60).
    /// </summary>
    public partial class PianoControl : UserControl
    {
        private const double WhiteWidth = 22;
        private const double WhiteHeight = 118;
        private const double BlackWidth = 13;
        private const double BlackHeight = 72;

        private static readonly Brush WhiteBrush = Brushes.White;
        private static readonly Brush BlackBrush = Brushes.Black;
        private static readonly Brush PressedBrush = Brushes.SteelBlue;

        private readonly Dictionary<int, Rectangle> keys = new();
        // hit-test order: black keys first, since they sit on top of the whites
        private readonly List<(int Note, Rect Bounds, bool Black)> hitOrder = new();
        private int draggingNote = -1;

        /// <summary>Lowest MIDI note shown (default C2 = 36).</summary>
        public int StartNote { get; set; } = 36;

        /// <summary>Highest MIDI note shown (default C6 = 84).</summary>
        public int EndNote { get; set; } = 84;

        /// <summary>Velocity used for on-screen key presses.</summary>
        public int Velocity { get; set; } = 100;

        /// <summary>Raised when the user starts playing a note. Argument is the MIDI note number.</summary>
        public event Action<int, int> NoteOn;

        /// <summary>Raised when the user stops playing a note. Argument is the MIDI note number.</summary>
        public event Action<int> NoteOff;

        public PianoControl()
        {
            InitializeComponent();
            Loaded += (_, _) => BuildKeyboard();
        }

        private static bool IsBlack(int note)
        {
            int pc = ((note % 12) + 12) % 12;
            return pc == 1 || pc == 3 || pc == 6 || pc == 8 || pc == 10;
        }

        private void BuildKeyboard()
        {
            keyCanvas.Children.Clear();
            keys.Clear();
            hitOrder.Clear();

            // white keys laid out left to right
            double x = 0;
            var whiteX = new Dictionary<int, double>();
            for (int note = StartNote; note <= EndNote; note++)
            {
                if (IsBlack(note)) continue;
                AddKey(note, x, 0, WhiteWidth - 1, WhiteHeight, WhiteBrush, false);
                whiteX[note] = x;
                x += WhiteWidth;
            }

            // black keys straddling the boundary between their two neighbouring whites
            for (int note = StartNote; note <= EndNote; note++)
            {
                if (!IsBlack(note)) continue;
                if (!whiteX.TryGetValue(note - 1, out double leftX)) continue;
                double bx = leftX + WhiteWidth - BlackWidth / 2;
                AddKey(note, bx, 0, BlackWidth, BlackHeight, BlackBrush, true);
            }

            keyCanvas.Width = x;
        }

        private void AddKey(int note, double x, double y, double w, double h, Brush fill, bool black)
        {
            var rect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = fill,
                Stroke = Brushes.DimGray,
                StrokeThickness = 1
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            keyCanvas.Children.Add(rect);
            keys[note] = rect;
            hitOrder.Insert(black ? 0 : hitOrder.Count, (note, new Rect(x, y, w, h), black));
        }

        private int HitTest(Point p)
        {
            foreach (var (note, bounds, _) in hitOrder)
                if (bounds.Contains(p)) return note;
            return -1;
        }

        private void SetPressed(int note, bool pressed)
        {
            if (!keys.TryGetValue(note, out var rect)) return;
            rect.Fill = pressed ? PressedBrush : (IsBlack(note) ? BlackBrush : WhiteBrush);
        }

        /// <summary>
        /// Lights or clears a key from an external source (e.g. incoming hardware
        /// MIDI). Safe to call for notes outside the displayed range (ignored).
        /// </summary>
        public void SetNoteState(int note, bool on) => SetPressed(note, on);

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            int note = HitTest(e.GetPosition(keyCanvas));
            if (note < 0) return;
            CaptureMouse();
            draggingNote = note;
            SetPressed(note, true);
            NoteOn?.Invoke(note, Velocity);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (draggingNote < 0 || e.LeftButton != MouseButtonState.Pressed) return;
            int note = HitTest(e.GetPosition(keyCanvas));
            if (note < 0 || note == draggingNote) return;
            SetPressed(draggingNote, false);
            NoteOff?.Invoke(draggingNote);
            draggingNote = note;
            SetPressed(note, true);
            NoteOn?.Invoke(note, Velocity);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (draggingNote < 0) return;
            SetPressed(draggingNote, false);
            NoteOff?.Invoke(draggingNote);
            draggingNote = -1;
            ReleaseMouseCapture();
        }
    }
}
