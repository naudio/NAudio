using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NAudioWpfDemo.SampleEditorDemo
{
    /// <summary>The four editable points on a single sample.</summary>
    public enum SampleMarker { Start, End, LoopStart, LoopEnd }

    /// <summary>
    /// Draws a (mono) waveform with four draggable markers — sample start/end and
    /// loop start/end — and a shaded loop region. Marker positions are sample
    /// indices; <see cref="MarkerMoved"/> fires as the user drags. The host sets the
    /// sample and the marker values; the control is otherwise self-contained.
    /// </summary>
    public partial class WaveformControl : UserControl
    {
        private const double HitTolerance = 6; // px either side of a marker line to grab it

        private float[] samples = Array.Empty<float>();
        private int sampleLength;
        private readonly Dictionary<SampleMarker, int> markers = new()
        {
            [SampleMarker.Start] = 0,
            [SampleMarker.End] = 0,
            [SampleMarker.LoopStart] = 0,
            [SampleMarker.LoopEnd] = 0
        };
        private SampleMarker? dragging;

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
            UpdateMarkers();
        }

        private int Clamp(int index) => index < 0 ? 0 : index > sampleLength ? sampleLength : index;

        private double IndexToX(int index) =>
            sampleLength <= 0 ? 0 : index / (double)sampleLength * canvas.ActualWidth;

        private int XToIndex(double x) =>
            sampleLength <= 0 ? 0 : Clamp((int)Math.Round(x / canvas.ActualWidth * sampleLength));

        private void Redraw()
        {
            canvas.Children.Clear();
            double w = canvas.ActualWidth, h = canvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            // shaded loop region (drawn first, beneath the waveform)
            double loopX1 = IndexToX(markers[SampleMarker.LoopStart]);
            double loopX2 = IndexToX(markers[SampleMarker.LoopEnd]);
            var loopRegion = new Rectangle
            {
                Width = Math.Max(0, loopX2 - loopX1),
                Height = h,
                Fill = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0))
            };
            Canvas.SetLeft(loopRegion, loopX1);
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

            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            // markers are redrawn together with the waveform; remove the old lines first
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
                if (canvas.Children[i] is Line) canvas.Children.RemoveAt(i);

            AddMarkerLine(SampleMarker.Start, Brushes.LimeGreen);
            AddMarkerLine(SampleMarker.End, Brushes.OrangeRed);
            AddMarkerLine(SampleMarker.LoopStart, Brushes.DarkOrange);
            AddMarkerLine(SampleMarker.LoopEnd, Brushes.DarkOrange);
        }

        private void AddMarkerLine(SampleMarker marker, Brush brush)
        {
            double x = IndexToX(markers[marker]);
            var line = new Line
            {
                X1 = x, X2 = x, Y1 = 0, Y2 = canvas.ActualHeight,
                Stroke = brush, StrokeThickness = 2
            };
            canvas.Children.Add(line);
        }

        private SampleMarker? HitTest(double x)
        {
            SampleMarker? best = null;
            double bestDist = HitTolerance;
            foreach (var kv in markers)
            {
                double dist = Math.Abs(IndexToX(kv.Value) - x);
                if (dist <= bestDist) { bestDist = dist; best = kv.Key; }
            }
            return best;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            dragging = HitTest(e.GetPosition(canvas).X);
            if (dragging != null) CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging == null || e.LeftButton != MouseButtonState.Pressed) return;
            int index = XToIndex(e.GetPosition(canvas).X);
            markers[dragging.Value] = index;
            UpdateMarkers();
            MarkerMoved?.Invoke(dragging.Value, index);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (dragging != null) { dragging = null; ReleaseMouseCapture(); }
        }
    }
}
