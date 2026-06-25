# NAudio.Sampler

[![Nuget](https://img.shields.io/nuget/v/NAudio.Sampler)](https://www.nuget.org/packages/NAudio.Sampler/)

A cross-platform software sampler for [NAudio](https://github.com/naudio/NAudio). Play MIDI through SoundFont (`.sf2`) and SFZ instruments, or a single sample mapped across the keyboard. Truly cross-platform — `net9.0`, no `[SupportedOSPlatform]`.

## What's included

- **SoundFont playback** — `SoundFontSampler` plays `.sf2` instruments, including the SF2 modulator engine
- **SFZ playback** — `SfzSampler` plays SFZ instruments, with `ISfzSampleLoader` / `FileSfzSampleLoader` for resolving the referenced sample files
- **Single-sample instruments** — `SingleSampleInstrument` / `SingleSampleSampler` map one sample across the keyboard
- **Voice engine** — a polyphonic `SamplerEngine` with DAHDSR envelopes, LFOs, modulated filters (`SamplerFilterType`), crossfade curves (`SamplerCrossfadeCurve`), trigger modes (`SamplerTrigger`) and reverb/chorus sends

Each sampler is exposed as an `ISampleProvider`, so it drops straight into any NAudio mixer or output pipeline.

## When to use it

Reach for this package when you want to render MIDI to audio using SoundFont or SFZ instruments, or build a sample-based instrument, on any platform NAudio runs on. It builds on `NAudio.SoundFile` for sample loading and is not pulled in by the `NAudio` meta-package — reference it explicitly:

```sh
dotnet add package NAudio.Sampler
```

## Tutorial

For a worked walkthrough see [the NAudio.Sampler tutorial](../Docs/Sampler.md).

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT.
