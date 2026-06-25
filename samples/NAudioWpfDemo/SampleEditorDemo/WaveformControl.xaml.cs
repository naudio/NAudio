using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAudioWpfDemo.SampleEditorDemo;

/// <summary>The four editable points on a single sample.</summary>
public enum SampleMarker { Start, End, LoopStart, LoopEnd }

/// <summary>
/// Draws a (mono) waveform with four draggable markers — sample start/end and
/// loop start/end — and a shaded loop region. Each marker has a triangular grab
/// handle: start/end handles sit at the top, loop handles at the bottom, so the
/// loop and sample markers don't fight for the same grab zone when they overlap.
/// Hit-testing keys off the cursor's vertical half: clicks in the top half move
/// start/end, clicks in the bottom half move the loop points. Marker positions
/// are sample indices; <see cref="MarkerMoved"/> fires as the user drags.
/// </summary>
public partial class WaveformControl : UserControl
{
    private const double HandleHalfWidth = 6;
    private const double HandleHeight = 11;
    private const double HitTolerance = 9; // px either side of a handle to grab it

    private float[] samples = Array.Empty<float>();
    private int sampleLength;
    private readonly Dictionary<SampleMarker, int> markers = new()
    {
        [SampleMarker.Start] = 0,
        [SampleMarker.End] = 0,
        [SampleMarker.LoopStart] = 0,
        [SampleMarker.LoopEnd] = 0
    };
    private Rectangle loopRegion;
    private SampleMarker? dragging;

    private static readonly Brush StartBrush = Brushes.LimeGreen;
    private static readonly Brush EndBrush = Brushes.OrangeRed;
    private static readonly Brush LoopBrush = Brushes.DarkOrange;
    private static readonly Brush PlayheadBrush = Brushes.DodgerBlue;
    private const string PlayheadTag = "playhead";

    /// <summary>Raised while a marker is dragged, with its new sample index.</summary>
    public event Action<SampleMarker, int> MarkerMoved;

    public WaveformControl()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    /// <summary>Sets the waveform to display (downmixed to mono by the caller).</summary>
    public void SetSample(float[] mono, int length)
    {
        samples = mono ?? Array.Empty<float>();
        sampleLength = Math.Max(0, length);
        Redraw();
    }

    /// <summary>Sets a marker's sample index without raising <see cref="MarkerMoved"/>.</summary>
    public void SetMarker(SampleMarker marker, int sampleIndex)
    {
        markers[marker] = Clamp(sampleIndex);
        UpdateOverlays();
    }

    /// <summary>
    /// Draws a playback-position line for each active voice (one per element of
    /// <paramref name="positions"/> up to <paramref name="count"/>, in source
    /// sample indices). Cheap enough to call at UI frame rate.
    /// </summary>
    public void SetPlayheads(double[] positions, int count)
    {
        // clear the previous playhead lines (leave the waveform/markers alone)
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
            if (canvas.Children[i] is Line line && Equals(line.Tag, PlayheadTag))
                canvas.Children.RemoveAt(i);

        double h = canvas.ActualHeight;
        if (h <= 0 || positions == null) return;
        for (int i = 0; i < count && i < positions.Length; i++)
        {
            double x = sampleLength <= 0 ? 0 : positions[i] / sampleLength * canvas.ActualWidth;
            canvas.Children.Add(new Line
            {
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = h,
                Stroke = PlayheadBrush,
                StrokeThickness = 1,
                Tag = PlayheadTag,
                IsHitTestVisible = false
            });
        }
    }

    private int Clamp(int index) => index < 0 ? 0 : index > sampleLength ? sampleLength : index;

    private double IndexToX(int index) =>
        sampleLength <= 0 ? 0 : index / (double)sampleLength * canvas.ActualWidth;

    private int XToIndex(double x) =>
        sampleLength <= 0 ? 0 : Clamp((int)Math.Round(x / canvas.ActualWidth * sampleLength));

    private static bool IsLoop(SampleMarker m) => m == SampleMarker.LoopStart || m == SampleMarker.LoopEnd;

    private void Redraw()
    {
        canvas.Children.Clear();
        loopRegion = null;
        double w = canvas.ActualWidth, h = canvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        // shaded loop region (drawn first, beneath the waveform)
        loopRegion = new Rectangle { Height = h, Fill = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0)) };
        canvas.Children.Add(loopRegion);

        // waveform: one vertical min..max bar per pixel column
        if (sampleLength > 0)
        {
            double mid = h / 2, half = h / 2;
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                int columns = (int)w;
                for (int col = 0; col < columns; col++)
                {
                    int from = (int)((long)col * sampleLength / columns);
                    int to = (int)((long)(col + 1) * sampleLength / columns);
                    if (to <= from) to = from + 1;
                    if (from >= sampleLength) break;
                    if (to > sampleLength) to = sampleLength;

                    float min = 0, max = 0;
                    for (int i = from; i < to; i++)
                    {
                        float s = samples[i];
                        if (s < min) min = s;
                        if (s > max) max = s;
                    }
                    ctx.BeginFigure(new Point(col, mid - max * half), false, false);
                    ctx.LineTo(new Point(col, mid - min * half), true, false);
                }
            }
            geometry.Freeze();
            canvas.Children.Add(new Path { Data = geometry, Stroke = Brushes.SteelBlue, StrokeThickness = 1 });
        }

        UpdateOverlays();
    }

    // repositions the loop shading and redraws the marker lines + handles; lighter
    // than a full Redraw so it can run on every drag move
    private void UpdateOverlays()
    {
        double w = canvas.ActualWidth, h = canvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        // remove the previous marker visuals (the waveform/loop region and the
        // playback playheads stay — playheads are tagged and owned by SetPlayheads)
        for (int i = canvas.Children.Count - 1; i >= 0; i--)
        {
            var child = canvas.Children[i];
            if (child is Polygon || (child is Line line && !Equals(line.Tag, PlayheadTag)))
                canvas.Children.RemoveAt(i);
        }

        if (loopRegion != null)
        {
            double lx1 = IndexToX(markers[SampleMarker.LoopStart]);
            double lx2 = IndexToX(markers[SampleMarker.LoopEnd]);
            Canvas.SetLeft(loopRegion, lx1);
            loopRegion.Width = Math.Max(0, lx2 - lx1);
        }

        AddMarker(SampleMarker.Start, StartBrush);
        AddMarker(SampleMarker.End, EndBrush);
        AddMarker(SampleMarker.LoopStart, LoopBrush);
        AddMarker(SampleMarker.LoopEnd, LoopBrush);
    }

    private void AddMarker(SampleMarker marker, Brush brush)
    {
        double x = IndexToX(markers[marker]);
        double h = canvas.ActualHeight;

        canvas.Children.Add(new Line
        {
            X1 = x,
            X2 = x,
            Y1 = 0,
            Y2 = h,
            Stroke = brush,
            StrokeThickness = 1.5
        });

        // triangle grab handle: start/end at the top, loop points at the bottom
        var handle = new Polygon { Fill = brush, Stroke = Brushes.Black, StrokeThickness = 0.5 };
        handle.Points = IsLoop(marker)
            ? new PointCollection { new(x - HandleHalfWidth, h), new(x + HandleHalfWidth, h), new(x, h - HandleHeight) }
            : new PointCollection { new(x - HandleHalfWidth, 0), new(x + HandleHalfWidth, 0), new(x, HandleHeight) };
        canvas.Children.Add(handle);
    }

    private SampleMarker? HitTest(Point p)
    {
        // top half grabs the sample start/end handles, bottom half the loop handles
        var candidates = p.Y < canvas.ActualHeight / 2
            ? new[] { SampleMarker.Start, SampleMarker.End }
            : new[] { SampleMarker.LoopStart, SampleMarker.LoopEnd };

        SampleMarker? best = null;
        double bestDist = HitTolerance;
        foreach (var marker in candidates)
        {
            double dist = Math.Abs(IndexToX(markers[marker]) - p.X);
            if (dist <= bestDist) { bestDist = dist; best = marker; }
        }
        return best;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        dragging = HitTest(e.GetPosition(canvas));
        if (dragging != null) CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging == null || e.LeftButton != MouseButtonState.Pressed) return;
        int index = XToIndex(e.GetPosition(canvas).X);
        markers[dragging.Value] = index;
        UpdateOverlays();
        MarkerMoved?.Invoke(dragging.Value, index);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (dragging != null) { dragging = null; ReleaseMouseCapture(); }
    }
}
