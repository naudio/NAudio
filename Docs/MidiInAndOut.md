# Sending and Receiving MIDI Events

NAudio allows you to send and receive MIDI events from MIDI devices using the `MidiIn` and `MidiOut` classes.

## Enumerating MIDI Devices

To discover how many devices are present in your system, you can use `MidiIn.NumberOfDevices` and `MidiOut.NumberOfDevices`. Then you can ask for information about each device using `MidiIn.DeviceInfo(index)` and `MidiOut.DeviceInfo(index)`. The `ProductName` property is most useful as it can be used to populate a combo box allowing users to select the device they want.

```c#
for (int device = 0; device < MidiIn.NumberOfDevices; device++)
{
    comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
}
if (comboBoxMidiInDevices.Items.Count > 0)
{
    comboBoxMidiInDevices.SelectedIndex = 0;
}
for (int device = 0; device < MidiOut.NumberOfDevices; device++)
{
    comboBoxMidiOutDevices.Items.Add(MidiOut.DeviceInfo(device).ProductName);
}
```

## Receiving MIDI events

To start monitoring incoming MIDI messages we create a new instance of `MidiIn` passing in the selected device index (zero based). Then we subscribe to the `MessageReceived` and `ErrorReceived` properties. Then we call `Start` to actually start receiving messages from the device.

```c#
midiIn = new MidiIn(selectedDeviceIndex);
midiIn.MessageReceived += midiIn_MessageReceived;
midiIn.ErrorReceived += midiIn_ErrorReceived;
midiIn.Start();
```

Both event handlers provide us with a `MidiInMessageEventArgs` which provides a `Timestamp` (in milliseconds), the parsed `MidiEvent` as well as the `RawMessage` (which can be useful if NAudio couldn't interpret the message)

```c#
void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
{
    log.WriteError(String.Format("Time {0} Message 0x{1:X8} Event {2}",
        e.Timestamp, e.RawMessage, e.MidiEvent));
}

void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
{
    log.WriteInfo(String.Format("Time {0} Message 0x{1:X8} Event {2}",
        e.Timestamp, e.RawMessage, e.MidiEvent));
}
```

To stop monitoring, simply call `Stop` on the MIDI in device. And also `Dispose` the device if you are finished with it.

```c#
midiIn.Stop();
midiIn.Dispose();
```

## Sending MIDI events

Sending MIDI events makes use of `MidiOut`. First, create an instance of `MidiOut` passing in the desired device number:

```c#
midiOut = new MidiOut(comboBoxMidiOutDevices.SelectedIndex);
```

Then you can create any MIDI messages using classes derived from `MidiEvent`. For example, you could create a `NoteOnEvent`. Note that timestamps and durations are ignored in this scenario - they only apply to events in a MIDI file.

```c#
int channel = 1;
int noteNumber = 50;
var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
```

To send the MIDI event, we need to call `GetAsShortMessage` on the `MidiEvent` and pass the resulting value to `MidiOut.Send`

```c#
midiOut.Send(noteOnEvent.GetAsShortMessage());
```

When you're done with sending MIDI events, simply `Dispose` the device.

```c#
midiOut.Dispose();
```