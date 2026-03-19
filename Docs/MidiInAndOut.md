# Sending and Receiving MIDI Events

NAudio allows you to send and receive MIDI events from MIDI devices using the `WinRTMidiIn` and `WinRTMidiOut` classes, which use the `Windows.Devices.Midi` API available on Windows 10 and later.

These classes are available when targeting `net8.0-windows10.0.19041.0` or later, and implement the `IMidiInput` and `IMidiOutput` interfaces.

## Enumerating MIDI Devices

Device enumeration is async and returns `DeviceInformation` objects with `Id` and `Name` properties:

```c#
var midiInDevices = await WinRTMidiIn.GetDevicesAsync();
foreach (var device in midiInDevices)
{
    comboBoxMidiInDevices.Items.Add(device.Name);
}

var midiOutDevices = await WinRTMidiOut.GetDevicesAsync();
foreach (var device in midiOutDevices)
{
    comboBoxMidiOutDevices.Items.Add(device.Name);
}
```

## Receiving MIDI events

To start monitoring incoming MIDI messages, create an instance of `WinRTMidiIn` using the device ID from enumeration. Then subscribe to the `MessageReceived` event and call `Start`:

```c#
var deviceId = midiInDevices[selectedIndex].Id;
midiInput = await WinRTMidiIn.CreateAsync(deviceId);
midiInput.MessageReceived += midiIn_MessageReceived;
midiInput.Start();
```

The event handler provides a `MidiInMessageEventArgs` with a `Timestamp` (in milliseconds), the parsed `MidiEvent`, and the `RawMessage`:

```c#
void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
{
    Console.WriteLine($"Time {e.Timestamp} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
}
```

To stop monitoring, call `Stop`. Dispose the device when finished:

```c#
midiInput.Stop();
midiInput.Dispose();
```

## Sending MIDI events

Create an instance of `WinRTMidiOut` using a device ID:

```c#
var deviceId = midiOutDevices[selectedIndex].Id;
midiOutput = await WinRTMidiOut.CreateAsync(deviceId);
```

Create MIDI messages using classes derived from `MidiEvent` and send them via `GetAsShortMessage`:

```c#
int channel = 1;
int noteNumber = 60;
var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
midiOutput.Send(noteOnEvent.GetAsShortMessage());
```

When done, dispose the device:

```c#
midiOutput.Dispose();
```

## Sending and Receiving Sysex Messages

Send a sysex message using `SendBuffer`:

```c#
byte[] sysexMessage = { 0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7 };
midiOutput.SendBuffer(sysexMessage);
```

Receive sysex messages by subscribing to `SysexMessageReceived`:

```c#
midiInput.SysexMessageReceived += (sender, e) =>
{
    byte[] sysexBytes = e.SysexBytes;
    // process sysex data
};
```

## Converting Between NAudio and WinRT MIDI Types

The `MidiMessageConverter` class provides bidirectional conversion between NAudio `MidiEvent` types and `Windows.Devices.Midi` message types:

```c#
// NAudio event → WinRT message (for sending via Windows.Devices.Midi directly)
var noteOn = new NoteOnEvent(0, 1, 60, 100, 50);
IMidiMessage winRtMessage = MidiMessageConverter.ToWinRTMessage(noteOn.GetAsShortMessage());

// WinRT message → NAudio event (for processing received messages)
MidiEvent midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);
```

## Coding Against the Interfaces

The `IMidiInput` and `IMidiOutput` interfaces allow you to write code that works with any MIDI implementation:

```c#
void SendNotes(IMidiOutput output, IEnumerable<NoteOnEvent> notes)
{
    foreach (var note in notes)
    {
        output.Send(note.GetAsShortMessage());
    }
}
```
