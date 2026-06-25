# NAudio.Vst3

[![Nuget](https://img.shields.io/nuget/v/NAudio.Vst3)](https://www.nuget.org/packages/NAudio.Vst3/)

VST 3® plug-in hosting for [NAudio](https://github.com/naudio/NAudio). Discover, load, and host VST 3 audio effects and instruments. Windows-only (`net9.0-windows`).

## What's included

- **Plug-in discovery** — `Vst3PluginScanner` enumerates the VST 3 modules installed in the standard search paths (`EnumerateInstalled()`) or in a folder you choose (`EnumerateIn`)
- **Plug-in hosting** — `Vst3Plugin` loads a `.vst3` module, processes audio, exposes latency/tail info, parameters, units and program lists, and saves/loads state and `.vstpreset` files
- **Audio integration** — `Vst3EffectSampleProvider` runs an effect plug-in inside an NAudio `ISampleProvider` chain; `Vst3InstrumentSampleProvider` and `Vst3MidiInstrument` drive an instrument plug-in from MIDI
- **Parameters & presets** — `Vst3ParameterCollection`, `Vst3Parameter`, `Vst3Unit`, `Vst3ProgramList` and `Vst3PresetContents` for automation and preset management
- **Editor UI** — `Vst3PluginView` for hosting a plug-in's native editor window

## When to use it

Reach for this package when you want to load third-party VST 3 effects (EQ, reverb, compression, …) or instruments (synths, samplers) and run them inside an NAudio signal chain. It is not pulled in by the `NAudio` meta-package — reference it explicitly:

```sh
dotnet add package NAudio.Vst3
```

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT. VST is a registered trademark of Steinberg Media Technologies GmbH; this package is an independent host and is not affiliated with or endorsed by Steinberg.
