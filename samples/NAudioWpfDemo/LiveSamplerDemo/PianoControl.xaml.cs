using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAudioWpfDemo.LiveSamplerDemo;

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
    private static readonly Brush RootBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); // amber

    private readonly Dictionary<int, Rectangle> keys = new();
    // hit-test order: black keys first, since they sit on top of the whites
    private readonly List<(int Note, Rect Bounds, bool Black)> hitOrder = new();
    private readonly HashSet<int> pressedNotes = new();
    private int draggingNote = -1;
    private int rootKey = -1;

    /// <summary>
    /// A note to mark as the instrument's root key (highlighted), or -1 for
    /// none. Lets a host (e.g. the single-sample editor) show which key plays
    /// the sample at its native pitch. The highlight yields to a pressed key.
    /// </summary>
    public int RootKey
    {
        get => rootKey;
        set
        {
            int old = rootKey;
            rootKey = value;
            RefreshKey(old);
            RefreshKey(rootKey);
        }
    }

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
            AddKey(note, x, 0, WhiteWidth - 1, WhiteHeight, false);
            whiteX[note] = x;
            x += WhiteWidth;
        }

        // black keys straddling the boundary between their two neighbouring whites
        for (int note = StartNote; note <= EndNote; note++)
        {
            if (!IsBlack(note)) continue;
            if (!whiteX.TryGetValue(note - 1, out double leftX)) continue;
            double bx = leftX + WhiteWidth - BlackWidth / 2;
            AddKey(note, bx, 0, BlackWidth, BlackHeight, true);
        }

        keyCanvas.Width = x;
    }

    private void AddKey(int note, double x, double y, double w, double h, bool black)
    {
        var rect = new Rectangle
        {
            Width = w,
            Height = h,
            Fill = ResolveFill(note),
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

    // a pressed key wins, then the root-key highlight, then the natural colour
    private Brush ResolveFill(int note) =>
        pressedNotes.Contains(note) ? PressedBrush
        : note == rootKey ? RootBrush
        : IsBlack(note) ? BlackBrush : WhiteBrush;

    private void RefreshKey(int note)
    {
        if (keys.TryGetValue(note, out var rect)) rect.Fill = ResolveFill(note);
    }

    private void SetPressed(int note, bool pressed)
    {
        if (pressed) pressedNotes.Add(note); else pressedNotes.Remove(note);
        RefreshKey(note);
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
