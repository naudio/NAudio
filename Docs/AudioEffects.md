# Audio Effects

NAudio ships a suite of audio effects in the `NAudio.Effects` namespace. It is **pure managed C#** with no platform dependencies, lives in **NAudio.Core**, and works anywhere NAudio runs (Windows, Linux, macOS). The effects process 32-bit floating point audio, so they slot naturally into the `ISampleProvider` pipeline.

There are two layers to be aware of:

* The reusable DSP **kernels** in `NAudio.Dsp` (`BiQuadFilter`, `DelayLine`, `EnvelopeFollower`, `Oversampler`, `LinkwitzRileyCrossover`, `PartitionedConvolver`, `Lfo`, …). These are stateless-ish building blocks you can use directly to build your own processing.
* The **streaming/effect layer** in `NAudio.Effects`: the `IAudioEffect` contract, the `AudioEffect` base class, and the `EffectChain` / `EffectSampleProvider` that adapt effects into the pull-model sample pipeline. The supplied effects (compressor, reverb, delay, …) are built on top of the DSP kernels.

This guide covers the streaming layer. Effects operate on **interleaved** `Span<float>` (so for stereo the buffer is L, R, L, R, …), are configured once with a `WaveFormat`, and never allocate on the steady-state processing path.

## Applying a single effect

The quickest way to use an effect is to wrap an existing `ISampleProvider` in an `EffectSampleProvider`. The effect is configured with the source's format in the constructor.

```c#
var audio = new AudioFileReader("example.mp3");
var reverb = new ReverbEffect { Mix = 0.25f };
var output = new EffectSampleProvider(audio, reverb);

var device = new WaveOutEvent();
device.Init(output);
device.Play();
```

`EffectSampleProvider.Effect` gives you back the effect so you can keep tweaking it while it plays.

## Building a chain

To run several effects in series, use `EffectChain`. It is itself an `ISampleProvider`, applies effects in the order they were added, and offers a fluent `Add`:

```c#
var chain = new EffectChain(audio)
    .Add(new GateEffect { ThresholdDb = -50f })
    .Add(new CompressorEffect { ThresholdDb = -18f, Ratio = 4f })
    .Add(new ReverbEffect { Mix = 0.2f });

device.Init(chain);
device.Play();
```

Each effect is configured with the source format as it is added, so the chain is ready to read immediately.

## Dry/wet mix and bypass

Every effect derived from `AudioEffect` (which is all of the supplied ones) gives you two controls for free:

* **`Mix`** — the dry/wet blend, `0f` (fully dry, effect inaudible) to `1f` (fully wet). Time-based effects default to a sensible partial mix (e.g. `ReverbEffect` and `DelayEffect` default to a mostly-dry mix; modulation effects default to 0.5).
* **`Bypass`** — `true` passes the input straight through.

Both are **click-free**: the transition is ramped, so you can toggle bypass or sweep the mix live without a pop. (Even while bypassed the effect keeps running internally with its output discarded, so un-bypassing doesn't click either.)

```c#
reverb.Mix = 0.4f;     // smoothly crossfades to the new blend
reverb.Bypass = true;  // smoothly ramps out, effect stays warm
```

## A tour of the available effects

All of these live in `NAudio.Effects`. Parameter units are shown in brackets; every effect also has the inherited `Mix` and `Bypass`.

### EQ / filtering

| Effect | Key parameters |
| --- | --- |
| `Equalizer` | Multi-band parametric EQ. Pass `EqualizerBand`s (each has `Type`, `Frequency` (Hz), `Q`, `GainDb`, `ShelfSlope`); band `Type` is one of `Peaking`, `LowShelf`, `HighShelf`, `LowPass`, `HighPass`, `Notch`, `BandPass`, `AllPass`. Call `Update()` after editing bands. |
| `GraphicEqualizer` | Fixed ISO bands over the `Equalizer` engine. `GraphicEqualizerLayout.TenBandOctave` or `ThirtyOneBandThirdOctave`; `SetBandGain(index, dB)` / `GetBandGain(index)`. |
| `MonoMakerEffect` | `Frequency` (Hz) — sums everything below it to mono (bass-mono). Stereo only. |
| `DcBlockerEffect` | `CutoffFrequency` (Hz) — first-order high-pass to remove DC/rumble. |

### Dynamics

| Effect | Key parameters |
| --- | --- |
| `CompressorEffect` | `ThresholdDb`, `Ratio`, `KneeDb`, `AttackMs`, `ReleaseMs`, `MakeUpGainDb`, `Detector` (`DetectorMode.Peak`/`Rms`), `RmsWindowMs`; `GainReductionDb` (read-only meter). |
| `LimiterEffect` | Brick-wall look-ahead limiter. `CeilingDb`, `ReleaseMs`, `LookaheadMs`, `TruePeak`, `OversampleFactor` (1/2/4); `GainReductionDb` meter. Reports look-ahead via `LatencySamples`. |
| `GateEffect` | Gate / downward expander. `ThresholdDb`, `RangeDb`, `Ratio`, `HysteresisDb`, `AttackMs`, `HoldMs`, `ReleaseMs`; `GainReductionDb` meter. |
| `MultibandCompressorEffect` | Linkwitz–Riley crossover into per-band compressors. Construct with crossover frequencies (Hz); edit `Bands` (each `MultibandCompressorBand` has `ThresholdDb`, `Ratio`, `AttackMs`, `ReleaseMs`, `MakeUpGainDb`, `GainReductionDb`). |
| `TransientShaperEffect` | `AttackDb`, `SustainDb` (boost/cut the onset vs body), `FastMs`, `SlowMs`. |
| `DeEsserEffect` | Split-band de-esser. `CrossoverFrequency` (Hz), `ThresholdDb`, `Ratio`, `AttackMs`, `ReleaseMs`; `GainReductionDb` meter. |

### Level, pan & stereo

| Effect | Key parameters |
| --- | --- |
| `GainEffect` | `GainDb` (dB) / `LinearGain`; smoothed, click-free level. |
| `PanEffect` | `Pan` (-1 … +1), constant-power. Stereo only. |
| `StereoWidthEffect` | `Width` (0 = mono, 1 = unchanged, up to 2 = wider) via mid/side. Stereo only. |

### Saturation / lo-fi

| Effect | Key parameters |
| --- | --- |
| `SaturationEffect` | `DriveDb`, `OutputGainDb`, `Curve` (`SaturationCurve.Tanh`/`Cubic`/`ArcTan`/`HardClip`), `OversampleFactor` (1/2/4). |
| `BitCrusherEffect` | `BitDepth` (1–32), `TargetSampleRate` (Hz, 0 = off), `Smoothing`. |

### Delay / modulation

| Effect | Key parameters |
| --- | --- |
| `DelayEffect` | `DelayMs`, `Feedback` (0 … <1), `Damping` (0–1), `PingPong`, `TempoSync` + `Tempo` + `Division` (`NoteDivision`); `EffectiveDelayMs` meter. |
| `ChorusEffect` | `BaseDelayMs`, `DepthMs`, `RateHz`, `Feedback`. `SyncToTempo(bpm, division)`. |
| `FlangerEffect` | `BaseDelayMs`, `DepthMs`, `RateHz`, `Feedback` (may be negative). `SyncToTempo(...)`. |
| `PhaserEffect` | `Stages` (1–24), `MinFrequency`, `MaxFrequency` (Hz), `RateHz`, `Feedback`. `SyncToTempo(...)`. |
| `TremoloEffect` | `Depth` (0–1), `RateHz`, `Waveform` (`LfoWaveform`), `AutoPan`. `SyncToTempo(...)`. |

### Reverb

| Effect | Key parameters |
| --- | --- |
| `ReverbEffect` | Freeverb (Schroeder–Moorer). `RoomSize`, `Damping`, `Width` (all 0–1). Low CPU baseline. |
| `FdnReverbEffect` | Feedback-delay-network reverb. `DecaySeconds` (RT60), `Size`, `Damping`, `ModulationDepthMs`, `ModulationRateHz`, `PreDelayMs`, `Width`. Higher quality. |
| `ConvolutionReverbEffect` | Partitioned FFT convolution. `SetImpulseResponse(float[])` (mono IR for all channels) or `SetImpulseResponse(float[][])` (per channel); `PartitionSize` (power of two). Reports `LatencySamples`. |

### Voice / comms

| Effect | Key parameters |
| --- | --- |
| `AutomaticGainControlEffect` | `TargetDb`, `MaxGainDb`, `MinGainDb`, `AttackMs`, `ReleaseMs`, `RmsWindowMs`, `UseVoiceDetection`; `GainDb` meter. |
| `NoiseSuppressionEffect` | Spectral suppressor. `Aggressiveness`, `SpectralFloor`, `NoiseAdaptation`, `FrameSize` (power of two). Reports `LatencySamples`. |
| `ComfortNoiseEffect` | `LevelDb`, `Tone` (0 bright … 1 dark). Adds a low noise floor after a gate/suppressor. |

### Pitch

| Effect | Key parameters |
| --- | --- |
| `PitchShiftEffect` | Phase-vocoder pitch shift (no tempo change). `PitchSemitones` (±12), `FftSize`, `Oversampling`. Reports `LatencySamples`. |

## Editing a chain while it plays

`EffectChain` is designed to be edited from a UI/control thread while the audio thread is calling `Read`. `Add`, `Insert`, `RemoveAt` and `Move` each publish a new immutable array with a single atomic write, so a concurrent `Read` always sees either the whole pre-edit chain or the whole post-edit chain — never a half-built state — and `Read` itself takes no lock. Edits are serialized against each other internally, so multiple editor threads are also safe. New effects are configured for the source format on the editing thread before they are published, so configuration never lands on the audio thread.

```c#
chain.Add(new DelayEffect());   // append
chain.Insert(0, new GainEffect()); // put a trim at the front
chain.Move(0, 2);               // reorder
chain.RemoveAt(1);              // drop one
```

`chain.Effects` gives a point-in-time snapshot (in processing order) for display.

## Latency and Reset

Some effects introduce processing latency — look-ahead limiting, FFT/partitioned designs, pitch shifting. Each reports it through `IAudioEffect.LatencySamples` (samples **per channel**, `0` for effects with no inherent delay). `EffectChain.LatencySamples` sums the chain, which is what you need for delay compensation:

```c#
int compensate = chain.LatencySamples;
```

`Reset()` clears an effect's internal state — delay lines, filter history, envelopes, reverb tails — so the next block starts as if from silence. Call `EffectChain.Reset()` when you reuse a chain on a discontinuous signal, e.g. after seeking the source, to avoid hearing the tail of the previous position bleed into the new one:

```c#
reader.CurrentTime = TimeSpan.FromSeconds(30);
chain.Reset();
```

## The parameter model

Most effects also implement `IParameterized`, which exposes their controls as a uniform `IReadOnlyList<EffectParameter>`. This lets a generic UI, preset system, serializer or automation host drive any effect without effect-specific code.

Each `EffectParameter` is a thin facade over one of the effect's typed properties — it doesn't store state, its getter/setter forward to the property, so the property stays the single source of truth. A parameter has a `Name`, `Kind` (`Continuous`, `Toggle`, `Choice` or `Meter`), `Unit`, `Minimum`/`Maximum`, `DefaultValue`, optional `Choices`, and a `Value` (clamped on write; ignored for read-only `Meter` parameters).

```c#
var comp = new CompressorEffect();
foreach (var p in comp.Parameters)
    Console.WriteLine($"{p.Name}: {p.Value}{p.Unit} ({p.Minimum}..{p.Maximum})");
```

Note that `Bypass` and `Mix` are **not** in the parameter list — they live on `AudioEffect` and a generic host surfaces them separately.

### Marshalling live edits to the audio thread

When an effect is running behind an audio callback, you don't want a UI thread mutating filter coefficients or resizing delay buffers mid-block. `ParameterDispatchQueue` (an `IParameterDispatch`) solves this: it's a lock-free single-producer/single-consumer queue. `Attach` an effect's parameters to it, and subsequent writes to `EffectParameter.Value` are clamped and *posted* rather than applied inline. The audio thread calls `Drain()` once at the top of each block to apply every pending write where there's no concurrent reader.

```c#
var queue = new ParameterDispatchQueue();
queue.Attach(comp);            // route comp's parameters through the queue

// control thread: this is now deferred, not applied inline
comp.Parameters[1].Value = 6f; // Ratio

// audio thread, top of the processing block:
queue.Drain();
```

While attached, the parameter getter optimistically returns the just-requested value so a two-way bound UI doesn't snap back before the audio thread has caught up. `Detach` restores inline application.

### Effects that are not `IParameterized`

A few effects deliberately do **not** implement `IParameterized` because they have a dynamic band list or an impulse-response input that a flat parameter list can't represent. A generic, parameter-driven host should special-case these with bespoke UI:

* `MultibandCompressorEffect` — variable number of bands, each with its own settings
* `Equalizer` — a dynamic list of `EqualizerBand`s
* `GraphicEqualizer` — a per-layout set of band gains (`SetBandGain`/`GetBandGain`)
* `ConvolutionReverbEffect` — needs an impulse response, not scalar parameters

(The WPF demo shows how to wrap a band-list effect as a fixed-parameter one — see `SevenBandEqEffect` below.)

## Writing your own effect

Subclass `AudioEffect` and implement two methods:

* `OnConfigure(WaveFormat format)` — allocate and size your sample-rate-dependent state (delay lines, filters). Called before the first process and again if the format changes. Use the `SampleRate` and `Channels` helpers from the base.
* `ProcessBlock(Span<float> buffer)` — transform the interleaved buffer in place into the **fully-wet** signal. The base class handles dry/wet mixing and bypass around this call, so don't implement those yourself.

Override `Reset()` (calling `base.Reset()`) if your effect has internal state to clear, and override `LatencySamples` if it adds delay. Optionally implement `IParameterized` to expose your controls.

```c#
public sealed class HalfGainEffect : AudioEffect
{
    protected override void OnConfigure(WaveFormat format) { }

    protected override void ProcessBlock(Span<float> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] *= 0.5f;
    }
}
```

Effects are most easily built by **composing** the `NAudio.Dsp` kernels (and even other effects). The WPF demo's `RealtimeEffectsDemo/CustomEffects` folder has two worked examples:

* `FilterEffect` — a cascadable HPF + LPF built directly from `BiQuadFilter` / `CrossfadingBiQuadFilter`, with click-free retuning and an `IParameterized` control list.
* `SevenBandEqEffect` — wraps the toolkit `Equalizer` (which takes a dynamic band list) as a fixed seven-band effect that a generic parameter panel *can* render, by exposing one `EffectParameter` per band.
