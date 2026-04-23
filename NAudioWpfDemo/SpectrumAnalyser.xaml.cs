using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NAudio.Dsp;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Interaction logic for SpectrumAnalyser.xaml.
    /// </summary>
    /// <remarks>
    /// <para>Rendering model: the bar data is drawn in <see cref="OnRender"/> directly via
    /// <see cref="DrawingContext"/>, and the control is invalidated at the display refresh rate
    /// via <see cref="CompositionTarget.Rendering"/>. This keeps peak-decay animation smooth
    /// (driven by the refresh tick, not by the FFT arrival rate) and avoids going through WPF's
    /// retained-mode graphics for the per-frame redraw.</para>
    ///
    /// <para>Signal-chain conventions (these are the common-sense bits the earlier version
    /// quietly got wrong):
    /// <list type="bullet">
    ///   <item>dB = 20 × log10(|bin|) — not 10 × log10 — so a 10× quieter bin reads as −20 dB.</item>
    ///   <item>Non-DC, non-Nyquist bins are doubled before magnitude, so a full-scale sine reads
    ///     as 0 dBFS instead of −6 dBFS. (This compensates for the symmetric spectrum splitting
    ///     a real sine's energy across +freq and −freq bins.)</item>
    ///   <item>X axis is logarithmic in frequency so bass and treble each get a fair share of
    ///     screen real estate.</item>
    ///   <item>Bins are aggregated into ~96 log-frequency bands; each band's bar shows the peak
    ///     bin magnitude inside it — peak (not average) matches what musical spectrum analyzers
    ///     look like.</item>
    /// </list></para>
    /// </remarks>
    public partial class SpectrumAnalyser : UserControl
    {
        private const int NumBands = 96;
        private const double MinFrequencyHz = 20.0;
        private const double MinDb = -80.0;
        private const double MaxDb = 0.0;
        // Exponential-moving-average factors. Higher = smoother / slower. Attack < release so rising
        // peaks jump in fast but fall off gently.
        private const double SmoothingAttackFactor = 0.35;
        private const double SmoothingReleaseFactor = 0.85;
        private const double PeakDecayDbPerSecond = 30.0;

        private readonly Typeface labelTypeface = new Typeface("Segoe UI");
        private readonly Brush backgroundBrush = Brushes.Black;
        private readonly Brush barBrush;
        private readonly Pen gridPen;
        private readonly Pen peakPen;
        private readonly Brush labelBrush;

        private int sampleRate = 44100;
        private int spectrumHalfLength;        // length of the meaningful half of fftResults (N/2 + 1 bins)
        private (int startBin, int endBin)[] bandBins;

        private readonly double[] rawDb = new double[NumBands];
        private readonly double[] smoothedDb = new double[NumBands];
        private readonly double[] peakDb = new double[NumBands];

        private DateTime lastRenderTick = DateTime.UtcNow;

        public SpectrumAnalyser()
        {
            InitializeComponent();

            // Gradient fill: hot colours at the top of the bar (high magnitudes), cooler at the bottom.
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x3B, 0x2E), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xC7, 0x29), 0.25));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x58, 0xE0, 0x3C), 0.55));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x27, 0x8C, 0x3B), 1.0));
            gradient.Freeze();
            barBrush = gradient;

            gridPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 0.5);
            gridPen.Freeze();

            peakPen = new Pen(Brushes.White, 1.25);
            peakPen.Freeze();

            labelBrush = new SolidColorBrush(Color.FromArgb(200, 220, 220, 220));
            labelBrush.Freeze();

            for (int b = 0; b < NumBands; b++)
            {
                rawDb[b] = smoothedDb[b] = peakDb[b] = MinDb;
            }

            SizeChanged += (_, _) => InvalidateVisual();
            Loaded += (_, _) => CompositionTarget.Rendering += OnRenderTick;
            Unloaded += (_, _) => CompositionTarget.Rendering -= OnRenderTick;
        }

        /// <summary>
        /// Sample rate of the source audio. Used to map FFT bins to frequencies for the log-scale
        /// x-axis. The display reconfigures its band layout when this changes.
        /// </summary>
        public int SampleRate
        {
            get => sampleRate;
            set
            {
                if (value <= 0 || value == sampleRate) return;
                sampleRate = value;
                RebuildBands();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Feeds a new FFT result into the display. Expected to be called on the UI thread — the
        /// caller (AudioPlaybackViewModel) already dispatches the FFT event onto the dispatcher.
        /// </summary>
        /// <param name="fftResults">Full-size complex FFT output. Only bins 0..N/2 are used; the
        /// upper half (conjugate-symmetric) is ignored.</param>
        public void Update(Complex[] fftResults)
        {
            int halfLen = fftResults.Length / 2 + 1;
            if (halfLen != spectrumHalfLength)
            {
                spectrumHalfLength = halfLen;
                RebuildBands();
            }
            if (bandBins == null) return;

            int nyquistBin = fftResults.Length / 2;
            for (int b = 0; b < NumBands; b++)
            {
                int startBin = bandBins[b].startBin;
                int endBin = Math.Min(bandBins[b].endBin, nyquistBin);
                double peakMag = 0.0;
                for (int k = startBin; k <= endBin; k++)
                {
                    // Real-input FFTs have their energy split between +freq and -freq bins; the
                    // upper half is a conjugate image of the lower. Doubling bins 1..N/2-1 recovers
                    // the true amplitude so a full-scale sine reads as 0 dBFS. DC (bin 0) and
                    // Nyquist (bin N/2) appear only once in a real spectrum and are not doubled.
                    double correction = (k == 0 || k == nyquistBin) ? 1.0 : 2.0;
                    double mag = correction *
                                 Math.Sqrt(fftResults[k].X * fftResults[k].X + fftResults[k].Y * fftResults[k].Y);
                    if (mag > peakMag) peakMag = mag;
                }
                rawDb[b] = peakMag > 1e-12 ? 20.0 * Math.Log10(peakMag) : MinDb;
            }
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            double dt = Math.Max(0.001, (now - lastRenderTick).TotalSeconds);
            lastRenderTick = now;

            // Advance smoothing and peak decay only when we have band data to animate.
            if (bandBins != null)
            {
                for (int b = 0; b < NumBands; b++)
                {
                    double raw = rawDb[b];
                    double current = smoothedDb[b];
                    double factor = raw > current ? SmoothingAttackFactor : SmoothingReleaseFactor;
                    smoothedDb[b] = current * factor + raw * (1.0 - factor);

                    double decayedPeak = peakDb[b] - PeakDecayDbPerSecond * dt;
                    peakDb[b] = Math.Max(decayedPeak, raw);
                    if (peakDb[b] < MinDb) peakDb[b] = MinDb;
                }
            }

            // Always invalidate so the background, gridlines and frequency labels draw even before
            // any FFT data has arrived (otherwise the visualisation looks broken at startup).
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            dc.DrawRectangle(backgroundBrush, null, new Rect(0, 0, w, h));

            double dbRange = MaxDb - MinDb;
            double plotBottom = h - 14; // reserve 14 px at the bottom for frequency labels
            double plotTop = 0.0;
            double plotHeight = plotBottom - plotTop;
            if (plotHeight <= 0) return;

            // dB grid lines + labels
            foreach (double dbMark in new[] { -20.0, -40.0, -60.0 })
            {
                double y = plotTop + (MaxDb - dbMark) / dbRange * plotHeight;
                dc.DrawLine(gridPen, new Point(0, y), new Point(w, y));
                var text = new FormattedText(
                    $"{dbMark:0} dB",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    labelTypeface,
                    10,
                    labelBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(text, new Point(3, y - text.Height));
            }

            // Bars + peak markers — only drawn once we have band data (requires a first FFT arrival
            // AND a configured sample rate). Gridlines and frequency labels below are drawn
            // regardless, so the empty visualisation still looks like an axis system at startup.
            if (bandBins != null)
            {
                double bandWidthPx = w / NumBands;
                double barGap = Math.Min(1.5, bandWidthPx * 0.15);
                double barWidth = Math.Max(1.0, bandWidthPx - barGap);
                for (int b = 0; b < NumBands; b++)
                {
                    double barDb = Math.Clamp(smoothedDb[b], MinDb, MaxDb);
                    double barH = (barDb - MinDb) / dbRange * plotHeight;
                    double x = b * bandWidthPx;
                    dc.DrawRectangle(barBrush, null, new Rect(x, plotBottom - barH, barWidth, barH));

                    double pkDb = Math.Clamp(peakDb[b], MinDb, MaxDb);
                    double pkY = plotBottom - (pkDb - MinDb) / dbRange * plotHeight;
                    dc.DrawLine(peakPen, new Point(x, pkY), new Point(x + barWidth, pkY));
                }
            }

            // Frequency labels along the bottom, positioned by the same log-frequency mapping
            // used when the bands were built.
            double nyquistHz = sampleRate / 2.0;
            double logMin = Math.Log10(MinFrequencyHz);
            double logMax = Math.Log10(nyquistHz);
            double logSpan = logMax - logMin;
            foreach (double freqHz in new[] { 100.0, 1000.0, 10000.0 })
            {
                if (freqHz > nyquistHz) continue;
                double x = (Math.Log10(freqHz) - logMin) / logSpan * w;
                string label = freqHz >= 1000 ? $"{freqHz / 1000:0} kHz" : $"{freqHz:0} Hz";
                var text = new FormattedText(
                    label,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    labelTypeface,
                    10,
                    labelBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                double textX = Math.Clamp(x - text.Width / 2, 0, w - text.Width);
                dc.DrawText(text, new Point(textX, h - text.Height));
            }
        }

        private void RebuildBands()
        {
            if (spectrumHalfLength <= 1 || sampleRate <= 0) return;

            // spectrumHalfLength = N/2 + 1 (bins 0 through Nyquist). Bin frequency = k * SR/N.
            int nyquistBin = spectrumHalfLength - 1;
            double binHz = (sampleRate / 2.0) / nyquistBin;
            double logMin = Math.Log10(MinFrequencyHz);
            double logMax = Math.Log10(sampleRate / 2.0);
            double logSpan = logMax - logMin;

            var bins = new (int startBin, int endBin)[NumBands];
            for (int b = 0; b < NumBands; b++)
            {
                double bandLoHz = Math.Pow(10, logMin + logSpan * b / NumBands);
                double bandHiHz = Math.Pow(10, logMin + logSpan * (b + 1) / NumBands);
                int binLo = Math.Max(1, (int)Math.Floor(bandLoHz / binHz));
                int binHi = Math.Min(nyquistBin, (int)Math.Ceiling(bandHiHz / binHz));
                if (binHi < binLo) binHi = binLo;
                bins[b] = (binLo, binHi);
            }
            bandBins = bins;

            // Reset display state so old values from a previous config don't linger.
            for (int b = 0; b < NumBands; b++)
            {
                rawDb[b] = smoothedDb[b] = peakDb[b] = MinDb;
            }
        }
    }
}
