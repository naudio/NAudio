# NAudio.Midi

[![Nuget](https://img.shields.io/nuget/v/NAudio.Midi)](https://www.nuget.org/packages/NAudio.Midi/)

MIDI support for [NAudio](https://github.com/naudio/NAudio). Cross-platform (`net8.0`) — contains the MIDI event model and MIDI file reader/writer.

## What's included

- `MidiFile` for reading Standard MIDI Files (SMF)
- `MidiEventCollection` and `MidiEvent` hierarchy (`NoteEvent`, `NoteOnEvent`, `ControlChangeEvent`, `PatchChangeEvent`, `TempoEvent`, `TimeSignatureEvent`, `MetaEvent`, `SysexEvent`, …)
- `MidiFileWriter` helpers to produce MIDI files from a `MidiEventCollection`
- Enumerations for General MIDI patches, drum notes, controller numbers, etc.

## What's **not** here

Sending or receiving live MIDI through `MidiIn` / `MidiOut` uses the Windows Multimedia API and lives in the [NAudio.WinMM](https://www.nuget.org/packages/NAudio.WinMM/) package.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials on working with MIDI files and events.

## License

MIT.
