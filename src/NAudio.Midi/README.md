# NAudio.Midi

[![Nuget](https://img.shields.io/nuget/v/NAudio.Midi)](https://www.nuget.org/packages/NAudio.Midi/)

MIDI support for [NAudio](https://github.com/naudio/NAudio). Dual-targets `net9.0` (cross-platform: the MIDI event model and file reader/writer) and `net9.0-windows10.0.19041.0` (adds WinRT live MIDI I/O on Windows).

## What's included

- `MidiFile` for reading Standard MIDI Files (SMF)
- `MidiEventCollection` and `MidiEvent` hierarchy (`NoteEvent`, `NoteOnEvent`, `ControlChangeEvent`, `PatchChangeEvent`, `TempoEvent`, `TimeSignatureEvent`, `MetaEvent`, `SysexEvent`, …)
- `MidiFileWriter` helpers to produce MIDI files from a `MidiEventCollection`
- Enumerations for General MIDI patches, drum notes, controller numbers, etc.
- **Windows only:** `WinRTMidiIn` / `WinRTMidiOut` — live MIDI input and output backed by the WinRT `Windows.Devices.Midi` API (present only in the `net9.0-windows` build; the cross-platform `net9.0` build omits them)

## What's **not** here

Sending or receiving live MIDI through the legacy winmm-backed `MidiIn` / `MidiOut` uses the Windows Multimedia API and lives in the [NAudio.WinMM](https://www.nuget.org/packages/NAudio.WinMM/) package. (The WinRT-backed `WinRTMidiIn` / `WinRTMidiOut` above are the modern alternative and ship here.)

See the [NAudio documentation site](https://naudio.github.io/NAudio/) for tutorials and the full API reference, or the [GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials on working with MIDI files and events.
## License

MIT.
