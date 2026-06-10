# Playing SoundFont, SFZ and single-sample instruments with NAudio.Sampler

`NAudio.Sampler` is a polyphonic software sampler for NAudio 3. It is **pure managed C#** with no platform dependencies, so it runs anywhere NAudio runs (Windows, Linux, macOS), and it renders 32-bit float stereo through the standard `ISampleProvider` pull model — the sampler drops straight into any NAudio playback graph or effect chain.

There are three ways to give it an instrument:

| Instrument source | Entry point | What it plays |
| --- | --- | --- |
| SoundFont 2 (`.sf2`) | `SoundFontSampler` | A full multi-timbral bank: 16 MIDI channels, banks/programs, GM percussion |
| SFZ (`.sfz`) | `SfzSampler` | A single instrument defined by an SFZ file plus its WAV/FLAC/Ogg samples |
| A single sample | `SingleSampleSampler` | One audio buffer mapped across the keyboard, with live-editable loop points and envelope |

All three are subclasses of the shared `SamplerEngine`, so they get the same voice engine: pitch from root key and tuning, looping, DAHDSR amplitude and modulation envelopes, two per-voice LFOs, modulated resonant filters, velocity handling, equal-power pan, voice stealing, exclusive (choke) groups, sustain-pedal hold, and shared reverb/chorus send buses.

Where things live:

* **`NAudio.Sampler`** (NuGet package) — the engine and the three samplers.
* **`NAudio.Midi`** — the MIDI playback hosts: `IMidiInstrument` (the seam every sampler implements), `MidiFileSequence`, `SequencedMidiPlayer`, `OfflineMidiRenderer` and `LiveMidiInstrument`. These work with *any* `IMidiInstrument`, not just the sampler.
* **`NAudio.Core`** — the file-format layers: the SoundFont parser and resolved-instrument model (`NAudio.SoundFont` namespace) and the SFZ parser and opcode semantics (`NAudio.Sfz` namespace).

## Playing a SoundFont

Load a `.sf2` with the `SoundFont` class and hand it to a `SoundFontSampler`. The constructor takes an optional output sample rate (default 44100) and maximum voice count (default 64):

```c#
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.SoundFont;
using NAudio.Wave;

var soundFont = new SoundFont("FluidR3_GM.sf2");
var sampler = new SoundFontSampler(soundFont);

// the engine is single-threaded, so wrap it in LiveMidiInstrument
// before sending it events from outside the audio thread
var live = new LiveMidiInstrument(sampler);

using var device = new WaveOutEvent();
device.Init(live);
device.Play();

live.NoteOn(0, 60, 100);   // middle C on channel 0, velocity 100
Thread.Sleep(1000);
live.NoteOff(0, 60);
```

The sampler consumes MIDI through `ProcessMidiEvent(MidiEvent)` (plus the `NoteOn`/`NoteOff` convenience methods) and produces audio through `Read`. Both must be called from the same thread — see [Live MIDI input](#live-midi-input) for why the example above wraps the sampler in `LiveMidiInstrument`. When a host such as `SequencedMidiPlayer` drives the sampler on the audio thread itself, no wrapper is needed.

`WaveOutEvent` is used in these examples for brevity; the sampler is just an `ISampleProvider`, so any output device works — `WasapiPlayer` on Windows, `AlsaOut` on Linux, or anything else from [Choose an output device type](OutputDeviceTypes.md).

### Channels, banks and percussion

`SoundFontSampler` is multi-timbral: all 16 MIDI channels play simultaneously, each with its own program, bank, controllers and pitch bend. Channel numbers on the `NoteOn`/`NoteOff`/`AllSoundOff` style methods are zero-based (0–15).

* **Bank select** follows SF2 semantics: the bank-select MSB (CC0) selects the SoundFont bank (SF2 `wBank`, 0–127). The LSB (CC32) is tracked but does not affect preset selection. A program in a missing bank falls back to bank 0 (GS "capital tone" style), then to any other melodic bank — never to a percussion kit.
* **Percussion** lives in SF2 bank 128 (`SoundFontSampler.PercussionBank`). The channel identified by the `PercussionChannel` property (default 9, i.e. MIDI channel 10) always resolves against the percussion bank regardless of bank-select messages: note numbers pick drums, the program picks the kit (falling back to the Standard Kit). Set `PercussionChannel = -1` to disable this for a non-GM bank.

### Master gain and effects

`MasterGain` scales the final mix (default 1.0). The shared reverb and chorus buses — fed by each voice's SF2 `reverbEffectsSend`/`chorusEffectsSend` generators and CC91/CC93 — are exposed as `Reverb` (a `ReverbEffect`) and `Chorus` (a `ChorusEffect`) so you can tune or bypass them:

```c#
sampler.MasterGain = 0.8f;
sampler.Reverb.RoomSize = 0.7f;
sampler.Chorus.Bypass = true;
```

`ActiveVoiceCount` reports how many voices are currently sounding, and `GetActivePlaybackPositions(double[])` fills a buffer with each sounding voice's read position (useful for UI playback indicators; safe to call from a UI thread).

## Playing a MIDI file

`MidiFileSequence` loads a standard MIDI file onto the sequencing core's event timeline, building a tempo map from the file's tempo events. From there you can either play it live or render it offline.

### Live playback

`SequencedMidiPlayer` plays the timeline on any `IMidiInstrument`, driven by a `Transport`. It dispatches each block's MIDI events at their exact frame offset, so timing is sample-accurate:

```c#
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Sequencing;
using NAudio.SoundFont;
using NAudio.Wave;

var sequence = MidiFileSequence.FromFile("song.mid");
var sampler = new SoundFontSampler(new SoundFont("FluidR3_GM.sf2"));

var transport = new Transport(sequence.TempoMap, sampler.WaveFormat.SampleRate);
var player = new SequencedMidiPlayer(transport, sequence.Timeline, sampler);

using var device = new WaveOutEvent();
device.Init(player);
device.Play();
transport.Play();
```

The `Transport` gives you position and seeking: `CurrentFrames`/`CurrentTicks` report the playback position, `SeekTicks`/`SeekFrames` move it, and `Stop`/`Play` pause and resume. While the transport is stopped the player still pulls the instrument, so envelope tails and reverb ring out naturally. After a seek, call the instrument's `AllSoundOff()` if you don't want notes that were sounding to carry across.

### Offline rendering

`OfflineMidiRenderer` renders a sequence through an instrument faster than real time, with no audio hardware — to a WAV file or an in-memory buffer. A `tailSeconds` parameter (default 2.0) leaves room for releases and reverb tails after the last event:

```c#
var sequence = MidiFileSequence.FromFile("song.mid");
var sampler = new SoundFontSampler(new SoundFont("FluidR3_GM.sf2"));

OfflineMidiRenderer.RenderToWaveFile(sequence, sampler, "song.wav");

// or render to an interleaved float buffer:
float[] audio = OfflineMidiRenderer.Render(sequence, sampler, tailSeconds: 3.0);
```

The render is deterministic: the same inputs produce the same output, which also makes it a good basis for automated tests.

## Live MIDI input

The sampler engine is **single-threaded by design**: `Read` and every MIDI entry point must be called from one thread, normally the audio thread. MIDI input callbacks arrive on other threads, so never call the engine directly from one.

`LiveMidiInstrument` is the bridge. It wraps any `IMidiInstrument` and exposes a thread-safe `Send(MidiEvent)` (plus `NoteOn`/`NoteOff` and a panic `AllSoundOff` that queues CC120 on every channel). Events are placed on a lock-free queue and drained on the audio thread at the start of each `Read`, so the wrapped instrument is only ever touched from one thread. Events take effect at the next block boundary, so the added latency is just your output buffer size.

```c#
var sampler = SfzSampler.FromFile(@"instruments\piano.sfz");
var live = new LiveMidiInstrument(sampler);

using var device = new WaveOutEvent();
device.Init(live);
device.Play();

// on Windows, forward a hardware keyboard via WinRTMidiIn (NAudio.Wasapi);
// any IMidiInput works the same way
var midiIn = await WinRTMidiIn.CreateAsync(deviceId);
midiIn.MessageReceived += (s, e) => live.Send(e.MidiEvent);
midiIn.Start();
```

`LiveMidiInstrument` is itself an `IMidiInstrument` (its `ProcessMidiEvent` is the thread-safe `Send`), so hosts can treat live-bridged and directly-driven instruments uniformly.

## SFZ instruments

`SfzSampler.FromFile` parses an `.sfz` file and loads its samples, resolving relative paths (and `default_path`) against the file's directory:

```c#
var sampler = SfzSampler.FromFile(@"instruments\piano.sfz");
Console.WriteLine($"{sampler.RegionCount} playable regions");
```

An SFZ file defines a single instrument (no banks or programs), so every MIDI channel plays the same region set. Regions whose sample is missing or cannot be decoded are dropped at load time rather than failing the whole instrument.

The parser handles `//` and `/* */` comments, the `#define`/`$variable` preprocessor, `#include`, sample paths with spaces, and the `<control>`/`<global>`/`<master>`/`<group>`/`<region>` hierarchy (opcodes merge down the hierarchy in the usual way). For more control — parsing SFZ text from memory, custom `#include` resolution, or supplying samples yourself — use `SfzParser.Parse`/`ParseFile` (in `NAudio.Sfz`) and the `SfzSampler(SfzInstrument, ISfzSampleLoader, ...)` constructor.

### Sample formats

* **WAV** is read natively, including stereo samples and loop points authored in a `smpl` chunk. Per the SFZ spec, an embedded loop makes `loop_mode` default to `loop_continuous` for that region; explicit `loop_start`/`loop_end`/`loop_mode` opcodes always win.
* **FLAC, Ogg Vorbis, Opus** (and anything else libsndfile reads) decode through `NAudio.SoundFile`, which `NAudio.Sampler` references. This needs a system **libsndfile** at runtime; if it is absent or a file cannot be decoded, the region is skipped gracefully rather than failing the load. The libsndfile path does not surface embedded loop points, so FLAC/Ogg samples need explicit `loop_start`/`loop_end` opcodes to loop.

Every sample is decoded fully into memory at load time — there is no disk streaming, so very large libraries cost RAM (roughly twice the 16-bit file size, as 32-bit float).

## Single-sample instruments

The simplest instrument: one audio buffer mapped across the whole keyboard at a chosen root key. `SingleSampleSampler.FromWaveFile` is the one-liner entry point:

```c#
var sampler = SingleSampleSampler.FromWaveFile("pluck.wav", rootKey: 60);
var live = new LiveMidiInstrument(sampler);
// init a player with live, then live.NoteOn(...) as usual
```

The `Instrument` property exposes a `SingleSampleInstrument` whose settings are plain mutable properties, so a UI can bind to them directly:

* mapping: `RootKey`, `LoKey`/`HiKey`, `LoVelocity`/`HiVelocity`
* playback range and looping: `Start`, `End`, `LoopMode` (`None`/`Continuous`/`UntilRelease`, an enum from `NAudio.Dsp`), `LoopStart`, `LoopEnd`, `LoopCrossfadeSeconds` (smooths a loop seam that doesn't fall on matching samples)
* tone: `TuneCents`, `VolumeDb`, `Pan`, `VelocityTrackingPercent`
* amplitude envelope: `DelaySeconds`, `AttackSeconds`, `HoldSeconds`, `DecaySeconds`, `SustainLevel`, `ReleaseSeconds`

```c#
var inst = sampler.Instrument;
inst.LoopMode = LoopMode.Continuous;
inst.LoopStart = 14_300;
inst.LoopEnd = 52_800;
inst.AttackSeconds = 0.01f;
inst.ReleaseSeconds = 0.4f;
```

Edits are heard on the **next note played** — the sampler rebuilds the region per note-on, so an interactive editor (like the Single-Sample Editor panel in the NAudioWpfDemo app) just changes properties and plays. You can also construct a `SingleSampleInstrument` directly from a float buffer (mono, or left + right channels), e.g. from a live recording.

## What is supported (and what is not)

The sampler targets a documented, useful subset of each format: SFZ v1 plus common extensions, and the SF2.04 generator/modulator model. This section is the definitive list — if an opcode or feature is not named as supported here, assume it is ignored.

### SFZ opcodes

**Supported:**

* **Mapping and selection:** `sample`, `lokey`/`hikey`/`key`, `lovel`/`hivel` — with note names (`c#4`, c4 = 60) accepted wherever a key is expected; `<control>` `default_path`, `note_offset`, `octave_offset`
* **Pitch:** `pitch_keycenter`, `tune` (alias `pitch`), `transpose`, `pitch_keytrack`
* **Amplitude:** `volume` (boosts above 0 dB included), `pan`, `amp_veltrack` (including negative values), `ampeg_delay`/`ampeg_attack`/`ampeg_hold`/`ampeg_decay`/`ampeg_sustain`/`ampeg_release`
* **Filter:** `cutoff`, `resonance`, `fil_type` — all four families: low-pass (`lpf_*`), high-pass (`hpf_*`), band-pass (`bpf_*`) and band-reject (`brf_*`). The filter is always **2-pole**: the 1-pole/4-pole/6-pole variants (`lpf_1p`, `lpf_4p`, …) are accepted but play with the 2-pole shape
* **Sample playback:** `offset`, `end` (inclusive, per the spec; an explicit `end=-1` disables the region), `loop_mode`/`loopmode` (`no_loop`, `one_shot`, `loop_continuous`, `loop_sustain`), `loop_start`/`loop_end` (inclusive; aliases `loopstart`/`loopend`), WAV `smpl`-chunk loop points as the default loop
* **Triggers and groups:** `trigger` (`attack`/`release`/`first`/`legato`), `rt_decay` (release samples attenuated by held time), `group`/`off_by` (directional choke groups)
* **Note-on selection:** keyswitches (`sw_lokey`/`sw_hikey`/`sw_last`/`sw_default` — keyswitch presses make no sound), round-robin (`seq_length`/`seq_position`), random layers (`lorand`/`hirand`, one draw per note-on so layers select consistently), CC gating (`loccN`/`hiccN`), CC triggers (`on_loccN`/`on_hiccN` — the region plays at its root key when the controller rises into the window)
* **Crossfades:** `xfin_lokey`/`xfin_hikey`, `xfout_lokey`/`xfout_hikey`, `xfin_lovel`/`xfin_hivel`, `xfout_lovel`/`xfout_hivel`, with `xf_keycurve`/`xf_velcurve` (`gain` or `power`); a layer faded to zero doesn't spawn a voice
* **Modulation:** `pitchlfo_freq`/`pitchlfo_depth`/`pitchlfo_delay` (vibrato), `amplfo_*` (tremolo), `fillfo_*` (filter LFO), `fileg_*` (filter envelope: delay/attack/hold/decay/sustain/release plus depth) and `pitcheg_*` (pitch envelope) — see the shared-source note below
* **Per-region EQ:** `eq1_freq`/`eq1_gain`/`eq1_bw` (and `eq2_*`, `eq3_*`) — up to three peaking bands per voice
* **Effect sends:** `effect1` (to the shared reverb bus) and `effect2` (to the shared chorus bus)
* **Stereo samples**, with per-channel filtering and pan as a balance

One engine limitation to know about: the modulation LFO and modulation envelope are each a **single source per voice**. A region that uses both `amplfo_*` and `fillfo_*` with different rates shares one LFO rate/shape (the amp LFO's settings win) while keeping independent depths; likewise a region using both `fileg_*` and `pitcheg_*` shares one envelope shape (the filter EG wins). `pitchlfo_*` has its own dedicated LFO and is unaffected.

**Not supported** (parsed where noted, but not honoured):

* `off_mode` — a choked region always cuts with a fast (~5 ms) fade; `off_mode=normal` (release via the amp envelope) is not honoured
* `polyphony` — per-region voice caps are not enforced; only the engine-wide `maxVoices` limit applies
* `amp_velcurve_N` velocity-curve points
* ARIA/SFZ v2 flex EGs (`eg01_*`, …) and `<curve>` tables
* `set_ccN` initial controller values
* loop-crossfade opcodes
* disk streaming — every sample decodes fully into memory

### SoundFont 2

**Honoured** (not merely parsed):

* The full preset-zone/instrument-zone generator accumulation per SF2.04 §9.4: instrument generators absolute, preset generators additive, global zones, default generator values, key/velocity-range intersection (`Preset.ResolveRegions()` exposes this resolution layer in `NAudio.SoundFont` if you want it without the sampler)
* **Loop modes:** no-loop, continuous, and loop-until-release-then-tail (`sampleModes`), with the start/end/start-loop/end-loop address offset generators, fine and coarse
* **Pitch:** `overridingRootKey`, `coarseTune`, `fineTune`, `scaleTuning`, and the sample header's pitch correction
* **Filter:** `initialFilterFc`, `initialFilterQ`, `modEnvToFilterFc`, `modLfoToFilterFc`
* **Envelopes:** the full six-stage DAHDSR volume and modulation envelopes (delay/attack/hold/decay/sustain/release, with the spec's timecent times and centibel/permille sustain levels)
* **LFOs:** both LFOs with frequency and start delay; `modLfoToPitch`, `vibLfoToPitch`, `modEnvToPitch`, `modLfoToVolume`
* **Amplitude:** `initialAttenuation`, `pan` (equal-power), `keynum` and `velocity` overrides
* `exclusiveClass` (choke groups, click-free fade, sparing sibling layers of the same note-on)
* **Effects sends:** `reverbEffectsSend`, `chorusEffectsSend`, rendered through the shared buses
* **Modulators:** the implicit default modulators of SF2.04 §8.4 (velocity→attenuation, velocity→filter cutoff, CC1 and channel pressure→vibrato depth, CC7/CC11→attenuation, CC10→pan, CC91/CC93→sends) plus file-defined modulators combined per §9.5 (a local modulator supersedes a global one with identical routing; instrument modulators replace defaults, preset modulators add), with the four source curves (linear, concave, convex, switch — each in all polarity/direction combinations) and the linear and absolute-value transforms. Modulators are re-evaluated against live controllers at control rate (64-sample blocks)
* **16- and 24-bit samples** (the `sm24` sub-chunk is read and combined)

**Known deviations:**

* The filter is a single 2-pole biquad low-pass. This matches the spec's intent but is not bit-compatible with any particular hardware or reference synth.
* Attenuation is **spec-literal**: `initialAttenuation` centibels are applied exactly as written. FluidSynth (and synths that copy it) scales attenuation by 0.4× to emulate EMU hardware, so banks voiced against FluidSynth may play quieter here than you are used to. Compensate with `MasterGain` if needed.
* `keynumToVolEnvHold`/`keynumToVolEnvDecay` (and the modulation-envelope equivalents) are parsed but not applied — envelope times do not track key number.
* Pitch-wheel handling is realised by the channel pitch-bend path rather than the modulator list, so a *file-defined* modulator whose destination is initial pitch is ignored.
* Poly (per-note key) pressure and NRPN modulator sources are not tracked — they evaluate as zero. Channel pressure, velocity, key number, CC and pitch wheel sources all work.
* GS/XG-style drum-bank selection (selecting a percussion kit on a channel other than the percussion channel via bank select) is not supported; percussion is fixed to `PercussionChannel`.
* Modulator destinations outside the real generator set (e.g. the "initial pitch" virtual destination) are ignored.
* SoundFonts whose samples live in hardware ROM load and play as silence (their regions are skipped).

### MIDI messages

The engine understands these channel messages (anything else is ignored):

* **Note on / note off** — a note-on with velocity 0 is treated as a note-off, per the MIDI spec
* **Program change**
* **Pitch bend** — with the bend range set by **RPN 0** (pitch-bend sensitivity); the default range is ±2 semitones
* **Channel pressure** (aftertouch) — an SF2 modulator source (vibrato depth by default)
* **Control change:**
  * CC0 / CC32 — bank select MSB/LSB (MSB selects the SF2 bank; the LSB is tracked but unused)
  * CC1 — modulation wheel (vibrato depth via the SF2 default modulator)
  * CC7 — channel volume
  * CC10 — pan
  * CC11 — expression
  * CC64 — sustain pedal (released notes are held until pedal-up; release triggers fire when the pedal rises)
  * CC91 / CC93 — reverb / chorus send level
  * CC120 — all sound off (immediate, with a short anti-click fade)
  * CC121 — reset all controllers
  * CC123 — all notes off (notes release naturally)
  * Every other controller's value is stored and visible to SF2 file-defined modulators and SFZ CC gates/triggers, even though it has no hard-wired meaning

Sysex and meta events are not consumed by the engine (`MidiFileSequence` drops them when loading a file, except Set Tempo which builds the tempo map), and polyphonic key pressure is ignored.
