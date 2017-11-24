# MidiEvent types in NAudio

`MidiEvent` is the base class for all MIDI events in NAudio. It has the following properties:

 - **Channel** - the MIDI channel number from 1 to 16
- **DeltaTime** - the number of ticks after the previous event in the MIDI file
- **AbsoluteTime** - the number of ticks from the start of the MIDI file (calculated by adding the deltas for all previous events)
- **CommandCode** - the `MidiCommandCode` indicating what type of MIDI event it is (e.g note on, note off)
    - note that a command code of `NoteOn` may actually be a note off message if its velocity is zero

## NoteEvent

`NoteEvent` is used to represent Note Off and Key After Touch messages. It is also the base class for `NoteOnEvent`.

It has the following properties
- **NoteNumber** the MIDI note number in the range 0-127
- **Velocity** the MIDI note velocity in the range 0-127. If the commanbd codew is NoteOn and the velocity is 0, then most synthesizers will interpret this as a note off event

## NoteOnEvent

`NoteOnEvent` inherits from `NoteEvent` and adds a property to track the associated note off event. This makes it easier to adjust the duration of a note, as the duration is found by comparing absolute times of the note on and off events. It also makes sure the associated note off event stays updated if the note number or channel properties change.

- **OffEvent** - a link to the associated note off event
- **NoteLength** - the note length in ticks. Adjusting this value will change the absolutetime of the associated note off event

## MetaEvent

`MetaEvent` is the base class for all MIDI meta events. The main property is **MetaEventType** which indicates which type of MIDI meta event it is. Most common meta event types have their own specialized class which are discussed next.

## TextEvent

`TextEvent` is used for all meta events whose data is text. Examples include markers, copyright messages, lyrics, track names as well as basic text events. The **Text** property allows you to access the text in these events.

## KeySignatureEvent

`KeySignatureEvent` exposes the raw `SharpsFlats` and `MajorMinor` properties.

## TempoEvent

The `TempoEvent` exposes both the raw `MicrosecondsPerQuarterNote` value from the MIDI event and also converts that into a `Tempo` expressed as beats per minute.

## TimeSignatureEvent

`TimeSignatureEvent` exposes `Numerator` (number of beats in a bar), `Denominator` (which is confusingly in 'beat units' so 1 means 2, 2 means 4 (crochet), 3 means 8 (quaver), 4 means 16 and 5 means 32), as well as `TicksInMetronomeClick` and `No32ndNotesInQuarterNote`.

## Other MIDI Event Types

- SysexEvent
- ChannelAfterTouchEvent
- PatchChangeEvent
- TrackSequenceNumberEvent
- RawMetaevent
- SmpteOffsetEvent
- SequeceSpecificEvent
- PitchWheelChangeEvent
