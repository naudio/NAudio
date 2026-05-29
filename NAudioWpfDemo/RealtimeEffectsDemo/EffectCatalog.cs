using System;
using System.Collections.Generic;
using NAudio.Effects;
using NAudioWpfDemo.RealtimeEffectsDemo.CustomEffects;

namespace NAudioWpfDemo.RealtimeEffectsDemo
{
    /// <summary>
    /// The set of effects the realtime harness can add to a chain (the ones that expose
    /// a generic <see cref="IParameterized"/> surface). Effects needing bespoke setup
    /// (parametric EQ band lists, convolution impulse responses) are intentionally
    /// excluded from the generic editor; the demo-only effects below
    /// (<see cref="SevenBandEqEffect"/>, <see cref="FilterEffect"/>,
    /// <see cref="ThreeBandCompressorEffect"/>) show how to compose those primitives as
    /// fixed-parameter effects that the auto-panel can render — and double as a smoke
    /// test of their underlying DSP.
    /// </summary>
    static class EffectCatalog
    {
        public sealed record Entry(string Name, Func<IAudioEffect> Create);

        public static IReadOnlyList<Entry> Entries { get; } = new[]
        {
            new Entry("Gain", () => new GainEffect()),
            new Entry("Pan", () => new PanEffect()),
            new Entry("Stereo Width", () => new StereoWidthEffect()),
            new Entry("DC Blocker", () => new DcBlockerEffect()),
            new Entry("Mono Maker", () => new MonoMakerEffect()),
            new Entry("Compressor", () => new CompressorEffect()),
            new Entry("Limiter", () => new LimiterEffect()),
            new Entry("Gate", () => new GateEffect()),
            new Entry("Transient Shaper", () => new TransientShaperEffect()),
            new Entry("De-Esser", () => new DeEsserEffect()),
            new Entry("Saturation", () => new SaturationEffect()),
            new Entry("Bit Crusher", () => new BitCrusherEffect()),
            new Entry("Delay", () => new DelayEffect()),
            new Entry("Chorus", () => new ChorusEffect()),
            new Entry("Flanger", () => new FlangerEffect()),
            new Entry("Phaser", () => new PhaserEffect()),
            new Entry("Tremolo", () => new TremoloEffect()),
            new Entry("Reverb (Freeverb)", () => new ReverbEffect()),
            new Entry("Reverb (FDN)", () => new FdnReverbEffect()),
            new Entry("Pitch Shift", () => new PitchShiftEffect()),
            new Entry("AGC", () => new AutomaticGainControlEffect()),
            new Entry("Noise Suppression", () => new NoiseSuppressionEffect()),
            new Entry("Comfort Noise", () => new ComfortNoiseEffect()),
            // Demo-only effects composed from the toolkit's primitives.
            new Entry("7-Band EQ (demo)", () => new SevenBandEqEffect()),
            new Entry("HPF + LPF Filter (demo)", () => new FilterEffect()),
            new Entry("3-Band Compressor (demo)", () => new ThreeBandCompressorEffect())
        };
    }
}
