# Exploring MIDI Files with MidiFile

The `MidiFile` class in NAudio allows you to open and examine the MIDI events in a standard MIDI file. It can also be used to create or update MIDI files, but this article focuses on reading.

## Opening a MIDI file

Opening a `MidiFile` is as simple as creating a new `MidiFile` object and passing in the path. You can choose to enable `strictMode` which will throw exceptions if various faults are found with the file such as note on events missing a paired note off or controller values out of range.

```c#
var strictMode = false;
var mf = new MidiFile(fileName, strictMode);
```

We can discover what MIDI file format the file is (Type 0 or type 1), as well as how many tracks are present and what the `DeltaTicksPerQuarterNote` value is.

```c#
Console.WriteLine("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}",
                mf.FileFormat, mf.Tracks, mf.DeltaTicksPerQuarterNote);
```

## Examining the MIDI events

The MIDI events can be accessed with the `Events` property, passing in the index of the track whose events you want to access. This gives you a `MidiEventCollection` you can iterate through.

All the events in the MIDI file will be represented by a class inheriting from `MidiEvent`. The `MidiFile` class will also have set an `AbsoluteTime` property on each note, which represents the timestamp of the MIDI event from the start of file in terms of delta ticks.

For note on events, `MidiFile` will also try to pair up the corresponding `NoteOffEvent` events. This allows you to see the duration of each note (which is simply the difference in time between the absolute time of the `NoteOffEvent` and `NoteOnEvent`.

Each `MidiEvent` has a `ToString` overload with basic information, so we can print out details of all the events in the file like this. (we don't print out the `NoteOffEvent` instances, because they are each paired to a `NoteOnEvent` which reports the duration)


```c#
for (int n = 0; n < mf.Tracks; n++)
{
    foreach (var midiEvent in mf.Events[n])
    {
        if(!MidiEvent.IsNoteOff(midiEvent))
        {
            Console.WriteLine("{0} {1}\r\n", ToMBT(midiEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote, timeSignature), midiEvent);
        }
    }
}
```

You'll see that a helper `ToMBT` method is being used above to convert the `AbsoluteTime` into a more helpful Measures Beats Ticks format. Here's a basic implementation (that doesn't take into account any possible time signature events that might take place)

```c#
private string ToMBT(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
{
    int beatsPerBar = timeSignature == null ? 4 : timeSignature.Numerator;
    int ticksPerBar = timeSignature == null ? ticksPerQuarterNote * 4 : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << timeSignature.Denominator);
    int ticksPerBeat = ticksPerBar / beatsPerBar;
    long bar = 1 + (eventTime / ticksPerBar);
    long beat = 1 + ((eventTime % ticksPerBar) / ticksPerBeat);
    long tick = eventTime % ticksPerBeat;
    return String.Format("{0}:{1}:{2}", bar, beat, tick);
}
```

Note that to get the `TimeSignatureEvent` needed by this function we can simply do something like:

```c#
var timeSignature = mf.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();
```

