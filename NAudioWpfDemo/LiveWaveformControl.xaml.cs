using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NAudioWpfDemo
{
    /// <summary>Which drawing style the waveform uses.</summary>
    public enum WaveformRenderStyle
    {
        /// <summary>Filled region between the min and max curves. The classic DAW look.</summary>
        Envelope,
        /// <summary>One vertical rectangle per display column, min-to-max.</summary>
        Bars,
        /// <summary>Two stroked curves, one tracing maxima and one tracing minima.</summary>
        MinMaxLines
    }

    /// <summary>Vertical amplitude mapping.</summary>
    public enum WaveformVerticalScale
    {
        /// <summary>Amplitude maps linearly to pixels.</summary>
        Linear,
        /// <summary>Amplitude is compressed logarithmically (dB). Useful for quiet material.</summary>
        Decibel
    }

    /// <summary>
    /// Scrolling live waveform display. Accepts a stream of min/max pairs (e.g. from
    /// <c>SampleAggregator.MaximumCalculated</c>) and renders them with a configurable style.
    /// </summary>
    /// <remarks>
    /// <para>Rendering model: every fed min/max pair is stored in a ring buffer. A
    /// <see cref="CompositionTarget.Rendering"/> tick re-renders the full visible window each frame
    /// via <see cref="OnRender"/> and <see cref="DrawingContext"/>. Because the display is rebuilt
    /// from history each frame, style and scale changes are instant and resizing just works.</para>
    /// <para>The x-axis maps the ring buffer newest-on-the-right, so new data scrolls in from the
    /// right and older data slides off the left — DAW/scope-standard, rather than the older
    /// "walking blank zone" pattern.</para>
    /// </remarks>
    public partial class LiveWaveformControl : UserControl
    {
        // Ring-buffer capacity is fixed and generous — we never need more columns than this on
        // screen. Each slot holds one min/max pair (~10 ms of audio at the default notification
        // rate), so 4096 slots cover ~40 s of scroll history.
        private const int RingCapacity = 4096;
        private const double DecibelFloor = -60.0;

        private readonly float[] ringMin = new float[RingCapacity];
        private readonly float[] ringMax = new float[RingCapacity];
        private int ringWriteIndex;
        private int ringCount;

        private readonly Brush backgroundBrush = Brushes.Black;
        private readonly Pen gridPen;
        private readonly Pen zeroLinePen;
        private readonly Pen strokePen;

        public LiveWaveformControl()
        {
            InitializeComponent();

            gridPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), 0.5);
            gridPen.Freeze();
            zeroLinePen = new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 0.75);
            zeroLinePen.Freeze();
            // MinMaxLines stroke colour: picked to pop against the black background regardless of
            // ambient Foreground, and to sit harmoniously with the default Fill gradient.
            strokePen = new Pen(new SolidColorBrush(Color.FromRgb(0x8E, 0xE8, 0xD4)), 1.25);
            strokePen.Freeze();

            Loaded += (_, _) => CompositionTarget.Rendering += OnRenderTick;
            Unloaded += (_, _) => CompositionTarget.Rendering -= OnRenderTick;
            SizeChanged += (_, _) => InvalidateVisual();
        }

        public static readonly DependencyProperty RenderStyleProperty = DependencyProperty.Register(
            nameof(RenderStyle), typeof(WaveformRenderStyle), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(WaveformRenderStyle.Envelope, FrameworkPropertyMetadataOptions.AffectsRender));

        public WaveformRenderStyle RenderStyle
        {
            get => (WaveformRenderStyle)GetValue(RenderStyleProperty);
            set => SetValue(RenderStyleProperty, value);
        }

        public static readonly DependencyProperty VerticalScaleProperty = DependencyProperty.Register(
            nameof(VerticalScale), typeof(WaveformVerticalScale), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(WaveformVerticalScale.Linear, FrameworkPropertyMetadataOptions.AffectsRender));

        public WaveformVerticalScale VerticalScale
        {
            get => (WaveformVerticalScale)GetValue(VerticalScaleProperty);
            set => SetValue(VerticalScaleProperty, value);
        }

        public static readonly DependencyProperty TopHalfOnlyProperty = DependencyProperty.Register(
            nameof(TopHalfOnly), typeof(bool), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool TopHalfOnly
        {
            get => (bool)GetValue(TopHalfOnlyProperty);
            set => SetValue(TopHalfOnlyProperty, value);
        }

        public static readonly DependencyProperty SamplesPerColumnProperty = DependencyProperty.Register(
            nameof(SamplesPerColumn), typeof(int), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender, null,
                (_, v) => Math.Max(1, Math.Min(16, (int)v))));

        /// <summary>How many fed min/max pairs are merged into one display column. 1 = finest.</summary>
        public int SamplesPerColumn
        {
            get => (int)GetValue(SamplesPerColumnProperty);
            set => SetValue(SamplesPerColumnProperty, value);
        }

        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(
            nameof(BarWidth), typeof(double), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender, null,
                (_, v) => Math.Max(1.0, Math.Min(20.0, (double)v))));

        /// <summary>Bar width in pixels (only used by the <see cref="WaveformRenderStyle.Bars"/> style).</summary>
        public double BarWidth
        {
            get => (double)GetValue(BarWidthProperty);
            set => SetValue(BarWidthProperty, value);
        }

        public static readonly DependencyProperty BarGapProperty = DependencyProperty.Register(
            nameof(BarGap), typeof(double), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, null,
                (_, v) => Math.Max(0.0, Math.Min(10.0, (double)v))));

        /// <summary>Gap between bars in pixels (only used by the <see cref="WaveformRenderStyle.Bars"/> style).</summary>
        public double BarGap
        {
            get => (double)GetValue(BarGapProperty);
            set => SetValue(BarGapProperty, value);
        }

        public static readonly DependencyProperty FillBetweenLinesProperty = DependencyProperty.Register(
            nameof(FillBetweenLines), typeof(bool), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>When true, <see cref="WaveformRenderStyle.MinMaxLines"/> also fills the region
        /// between the min and max curves using <see cref="Fill"/>. No effect on other styles.</summary>
        public bool FillBetweenLines
        {
            get => (bool)GetValue(FillBetweenLinesProperty);
            set => SetValue(FillBetweenLinesProperty, value);
        }

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill), typeof(Brush), typeof(LiveWaveformControl),
            new FrameworkPropertyMetadata(BuildDefaultFill(), FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Brush used for filled regions (envelope fill, bar fill).</summary>
        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        private static Brush BuildDefaultFill()
        {
            var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            g.GradientStops.Add(new GradientStop(Color.FromRgb(0x58, 0xE0, 0x9C), 0.0));
            g.GradientStops.Add(new GradientStop(Color.FromRgb(0x29, 0xB6, 0xB2), 0.5));
            g.GradientStops.Add(new GradientStop(Color.FromRgb(0x1B, 0x6C, 0x88), 1.0));
            g.Freeze();
            return g;
        }

        /// <summary>Feeds a new peak envelope sample (min/max over a short window).</summary>
        public void AddValue(float maxValue, float minValue)
        {
            ringMin[ringWriteIndex] = minValue;
            ringMax[ringWriteIndex] = maxValue;
            ringWriteIndex = (ringWriteIndex + 1) % RingCapacity;
            if (ringCount < RingCapacity) ringCount++;
        }

        /// <summary>Clears all accumulated history.</summary>
        public void Reset()
        {
            ringWriteIndex = 0;
            ringCount = 0;
            InvalidateVisual();
        }

        private void OnRenderTick(object sender, EventArgs e) => InvalidateVisual();

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            dc.DrawRectangle(backgroundBrush, null, new Rect(0, 0, w, h));

            bool topHalf = TopHalfOnly;
            double baseline = topHalf ? h - 1 : h / 2.0;
            double halfHeight = topHalf ? h - 1 : h / 2.0;

            // Gridlines: full-scale frame, plus a centre/zero reference for the full-height mode.
            if (topHalf)
            {
                dc.DrawLine(zeroLinePen, new Point(0, baseline), new Point(w, baseline));
                dc.DrawLine(gridPen, new Point(0, baseline - halfHeight / 2), new Point(w, baseline - halfHeight / 2));
            }
            else
            {
                dc.DrawLine(zeroLinePen, new Point(0, baseline), new Point(w, baseline));
                dc.DrawLine(gridPen, new Point(0, baseline - halfHeight / 2), new Point(w, baseline - halfHeight / 2));
                dc.DrawLine(gridPen, new Point(0, baseline + halfHeight / 2), new Point(w, baseline + halfHeight / 2));
            }

            if (ringCount == 0) return;

            var style = RenderStyle;
            double columnStride = style == WaveformRenderStyle.Bars ? Math.Max(1.0, BarWidth + BarGap) : 1.0;
            int columns = Math.Max(1, (int)Math.Floor(w / columnStride));
            int samplesPerColumn = Math.Max(1, SamplesPerColumn);

            // How many ring-buffer entries do we need to render? (columns * samplesPerColumn)
            int needed = columns * samplesPerColumn;
            int available = Math.Min(needed, ringCount);
            int usedColumns = available / samplesPerColumn;
            if (usedColumns == 0) return;

            // Oldest ring index for what we're about to draw.
            int oldestIndex = (ringWriteIndex - usedColumns * samplesPerColumn + RingCapacity) % RingCapacity;

            // Newest column sits at the right edge, older columns extend leftward. We plot each
            // column from the right so a partially-filled ring still starts flush against the
            // right-hand side (music arriving).
            double rightEdge = w;

            switch (style)
            {
                case WaveformRenderStyle.Envelope:
                    DrawEnvelope(dc, oldestIndex, samplesPerColumn, usedColumns, rightEdge, columnStride, baseline, halfHeight, topHalf);
                    break;
                case WaveformRenderStyle.Bars:
                    DrawBars(dc, oldestIndex, samplesPerColumn, usedColumns, rightEdge, baseline, halfHeight, topHalf);
                    break;
                case WaveformRenderStyle.MinMaxLines:
                    DrawMinMaxLines(dc, oldestIndex, samplesPerColumn, usedColumns, rightEdge, columnStride, baseline, halfHeight, topHalf);
                    break;
            }
        }

        private void DrawEnvelope(DrawingContext dc, int oldestIndex, int samplesPerColumn, int columns,
            double rightEdge, double columnStride, double baseline, double halfHeight, bool topHalf)
        {
            var geom = BuildEnvelopeGeometry(oldestIndex, samplesPerColumn, columns, rightEdge, columnStride, baseline, halfHeight, topHalf);
            dc.DrawGeometry(Fill, null, geom);
        }

        private Geometry BuildEnvelopeGeometry(int oldestIndex, int samplesPerColumn, int columns,
            double rightEdge, double columnStride, double baseline, double halfHeight, bool topHalf)
        {
            double leftEdge = rightEdge - columns * columnStride;
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                // Top edge, left-to-right.
                ctx.BeginFigure(new Point(leftEdge, baseline), isFilled: true, isClosed: true);
                for (int c = 0; c < columns; c++)
                {
                    GetColumnMinMax(oldestIndex, samplesPerColumn, c, out float colMin, out float colMax);
                    double x = leftEdge + c * columnStride;
                    double yTop = TopYFor(colMin, colMax, baseline, halfHeight, topHalf);
                    ctx.LineTo(new Point(x, yTop), isStroked: false, isSmoothJoin: true);
                }
                // Close across the right edge, then bottom edge right-to-left back to the start.
                for (int c = columns - 1; c >= 0; c--)
                {
                    GetColumnMinMax(oldestIndex, samplesPerColumn, c, out float colMin, out float colMax);
                    double x = leftEdge + c * columnStride;
                    double yBot = BottomYFor(colMin, colMax, baseline, halfHeight, topHalf);
                    ctx.LineTo(new Point(x, yBot), isStroked: false, isSmoothJoin: true);
                }
            }
            geom.Freeze();
            return geom;
        }

        private void DrawBars(DrawingContext dc, int oldestIndex, int samplesPerColumn, int columns,
            double rightEdge, double baseline, double halfHeight, bool topHalf)
        {
            double barW = Math.Max(1.0, BarWidth);
            double gap = Math.Max(0.0, BarGap);
            double stride = barW + gap;
            double leftEdge = rightEdge - columns * stride;
            var brush = Fill;
            for (int c = 0; c < columns; c++)
            {
                GetColumnMinMax(oldestIndex, samplesPerColumn, c, out float colMin, out float colMax);
                double yTop = TopYFor(colMin, colMax, baseline, halfHeight, topHalf);
                double yBot = BottomYFor(colMin, colMax, baseline, halfHeight, topHalf);
                double height = Math.Max(1.0, yBot - yTop);
                dc.DrawRectangle(brush, null, new Rect(leftEdge + c * stride, yTop, barW, height));
            }
        }

        private void DrawMinMaxLines(DrawingContext dc, int oldestIndex, int samplesPerColumn, int columns,
            double rightEdge, double columnStride, double baseline, double halfHeight, bool topHalf)
        {
            if (FillBetweenLines)
            {
                var fillGeom = BuildEnvelopeGeometry(oldestIndex, samplesPerColumn, columns, rightEdge, columnStride, baseline, halfHeight, topHalf);
                dc.DrawGeometry(Fill, null, fillGeom);
            }

            double leftEdge = rightEdge - columns * columnStride;
            var topGeom = new StreamGeometry();
            var botGeom = new StreamGeometry();
            using (var topCtx = topGeom.Open())
            using (var botCtx = botGeom.Open())
            {
                for (int c = 0; c < columns; c++)
                {
                    GetColumnMinMax(oldestIndex, samplesPerColumn, c, out float colMin, out float colMax);
                    double x = leftEdge + c * columnStride;
                    double yTop = TopYFor(colMin, colMax, baseline, halfHeight, topHalf);
                    double yBot = BottomYFor(colMin, colMax, baseline, halfHeight, topHalf);
                    if (c == 0)
                    {
                        topCtx.BeginFigure(new Point(x, yTop), isFilled: false, isClosed: false);
                        botCtx.BeginFigure(new Point(x, yBot), isFilled: false, isClosed: false);
                    }
                    else
                    {
                        topCtx.LineTo(new Point(x, yTop), isStroked: true, isSmoothJoin: true);
                        botCtx.LineTo(new Point(x, yBot), isStroked: true, isSmoothJoin: true);
                    }
                }
            }
            topGeom.Freeze();
            botGeom.Freeze();
            dc.DrawGeometry(null, strokePen, topGeom);
            // In TopHalfOnly mode the min line collapses onto the baseline, so skip it.
            if (!topHalf)
            {
                dc.DrawGeometry(null, strokePen, botGeom);
            }
        }

        private void GetColumnMinMax(int oldestIndex, int samplesPerColumn, int column, out float colMin, out float colMax)
        {
            int start = (oldestIndex + column * samplesPerColumn) % RingCapacity;
            colMin = ringMin[start];
            colMax = ringMax[start];
            for (int s = 1; s < samplesPerColumn; s++)
            {
                int idx = (start + s) % RingCapacity;
                if (ringMin[idx] < colMin) colMin = ringMin[idx];
                if (ringMax[idx] > colMax) colMax = ringMax[idx];
            }
        }

        private double TopYFor(float colMin, float colMax, double baseline, double halfHeight, bool topHalf)
        {
            if (topHalf)
            {
                // Rectified peak: take whichever excursion was larger in absolute value.
                float peak = Math.Max(Math.Abs(colMax), Math.Abs(colMin));
                return baseline - MapAmplitude(peak, halfHeight);
            }
            // Positive peak goes above centre.
            float top = colMax > 0 ? colMax : 0f;
            return baseline - MapAmplitude(top, halfHeight);
        }

        private double BottomYFor(float colMin, float colMax, double baseline, double halfHeight, bool topHalf)
        {
            if (topHalf)
            {
                return baseline;
            }
            // Negative peak goes below centre. We flip the sign so MapAmplitude sees a positive number.
            float bot = colMin < 0 ? -colMin : 0f;
            return baseline + MapAmplitude(bot, halfHeight);
        }

        private double MapAmplitude(float magnitude, double halfHeight)
        {
            if (magnitude <= 0f) return 0.0;
            if (magnitude > 1f) magnitude = 1f;
            if (VerticalScale == WaveformVerticalScale.Linear)
            {
                return magnitude * halfHeight;
            }
            // Decibel: map [-60..0] dB to [0..halfHeight]. Anything below the floor clips to 0.
            double db = 20.0 * Math.Log10(magnitude);
            if (db <= DecibelFloor) return 0.0;
            return (db - DecibelFloor) / (0.0 - DecibelFloor) * halfHeight;
        }
    }
}
