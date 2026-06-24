using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Saturation transfer-curve shape.
    /// </summary>
    public enum SaturationCurve
    {
        /// <summary>Hyperbolic tangent — smooth, tube-like.</summary>
        Tanh,
        /// <summary>Cubic soft clip — gentle odd-harmonic colouration.</summary>
        Cubic,
        /// <summary>Arctangent — soft, slightly brighter than tanh.</summary>
        ArcTan,
        /// <summary>Hard clip — aggressive, square-ish.</summary>
        HardClip
    }

    /// <summary>
    /// Wave-shaping saturation / soft-clip with selectable curve, input drive and
    /// output trim. Optional 2× or 4× oversampling moves the harmonics the non-linearity
    /// generates above the audible band before they can alias back down.
    /// </summary>
    public sealed class SaturationEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Drive", "dB", 0f, 36f, () => DriveDb, v => DriveDb = v),
            EffectParameter.Continuous("Output", "dB", -24f, 24f, () => OutputGainDb, v => OutputGainDb = v),
            EffectParameter.Choice("Curve", new[] { "Tanh", "Cubic", "ArcTan", "Hard Clip" },
                () => (int)Curve, i => Curve = (SaturationCurve)i),
            EffectParameter.Choice("Oversample", new[] { "1x", "2x", "4x" },
                () => OversampleFactor == 4 ? 2 : OversampleFactor == 2 ? 1 : 0,
                i => OversampleFactor = i == 2 ? 4 : i == 1 ? 2 : 1)
        };

        private Oversampler[] oversamplers = Array.Empty<Oversampler>();
        private int oversampleFactor = 2;

        /// <summary>Input drive in dB applied before the curve. Default 6 dB.</summary>
        public float DriveDb { get; set; } = 6f;

        /// <summary>Output trim in dB applied after the curve. Default 0 dB.</summary>
        public float OutputGainDb { get; set; }

        /// <summary>Transfer curve. Default <see cref="SaturationCurve.Tanh"/>.</summary>
        public SaturationCurve Curve { get; set; } = SaturationCurve.Tanh;

        /// <summary>Oversampling factor: 1, 2 or 4. Default 2.</summary>
        public int OversampleFactor
        {
            get => oversampleFactor;
            set
            {
                oversampleFactor = value >= 4 ? 4 : value >= 2 ? 2 : 1;
                if (WaveFormat != null)
                    BuildOversamplers();
            }
        }

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format) => BuildOversamplers();

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            var drive = MathF.Pow(10f, DriveDb * (1f / 20f));
            var outGain = MathF.Pow(10f, OutputGainDb * (1f / 20f));
            var factor = oversampleFactor;
            Span<float> work = stackalloc float[4];

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                for (var ch = 0; ch < channels; ch++)
                {
                    var x = buffer[i + ch] * drive;
                    float y;
                    if (factor == 1)
                    {
                        y = Shape(x);
                    }
                    else
                    {
                        var os = oversamplers[ch];
                        os.Upsample(x, work);
                        for (var k = 0; k < factor; k++)
                            work[k] = Shape(work[k]);
                        y = os.Downsample(work[..factor]);
                    }
                    buffer[i + ch] = y * outGain;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            foreach (var os in oversamplers)
                os.Reset();
        }

        private float Shape(float x) => Curve switch
        {
            SaturationCurve.Cubic => CubicClip(x),
            SaturationCurve.ArcTan => 0.63661977f * MathF.Atan(x), // 2/pi
            SaturationCurve.HardClip => x < -1f ? -1f : x > 1f ? 1f : x,
            _ => MathF.Tanh(x)
        };

        private static float CubicClip(float x)
        {
            if (x <= -1f)
                return -2f / 3f;
            if (x >= 1f)
                return 2f / 3f;
            return x - x * x * x / 3f;
        }

        private void BuildOversamplers()
        {
            oversamplers = new Oversampler[Channels];
            for (var ch = 0; ch < Channels; ch++)
                oversamplers[ch] = new Oversampler(oversampleFactor, SampleRate);
        }
    }
}
