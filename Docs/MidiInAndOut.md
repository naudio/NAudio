# Sending and Receiving MIDI Events

NAudio ships two MIDI backends on Windows. Both implement the same `IMidiInput` / `IMidiOutput` interfaces (in `NAudio.Midi`), so application code can target the interfaces and switch backends with one line of construction.

| Backend | Package | Class | Notes |
| --- | --- | --- | --- |
| WinRT (`Windows.Devices.Midi`) | `NAudio.Wasapi` | `WinRTMidiIn`, `WinRTMidiOut` | Recommended. Async device enumeration, full `TimeSpan` timestamp resolution. Requires `net9.0-windows10.0.19041.0` or later. |
| Legacy winmm (`midiIn*` / `midiOut*`) | `NAudio.WinMM` | `MidiIn`, `MidiOut` | Synchronous, index-based device enumeration. Timestamps are millisecond-resolution. Also fires an `ErrorReceived` event for malformed messages. |

Application code can be backend-agnostic by referencing only the interfaces:

```c#
void Monitor(IMidiInput input)
{
    input.MessageReceived += (s, e) => Console.WriteLine($"{e.Timestamp:c} {e.MidiEvent}");
    input.Start();
}
```

## Enumerating MIDI devices

### WinRT backend

Device enumeration is asynchronous and returns `DeviceInformation` objects with `Id` and `Name` properties:

```c#
var inDevices = await WinRTMidiIn.GetDevicesAsync();
foreach (var device in inDevices)
{
    comboBoxMidiInDevices.Items.Add(device.Name);
}

var outDevices = await WinRTMidiOut.GetDevicesAsync();
foreach (var device in outDevices)
{
    comboBoxMidiOutDevices.Items.Add(device.Name);
}
```

### Legacy winmm backend

Synchronous, index-based:

```c#
for (int device = 0; device < MidiIn.NumberOfDevices; device++)
{
    comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
}
for (int device = 0; device < MidiOut.NumberOfDevices; device++)
{
    comboBoxMidiOutDevices.Items.Add(MidiOut.DeviceInfo(device).ProductName);
}
```

## Receiving MIDI events

### Opening a WinRT input port

```c#
IMidiInput midiIn = await WinRTMidiIn.CreateAsync(inDevices[selectedIndex].Id);
midiIn.MessageReceived += midiIn_MessageReceived;
midiIn.SysexMessageReceived += midiIn_SysexMessageReceived;
midiIn.Start();
```

### Opening a legacy winmm input port

```c#
IMidiInput midiIn = new MidiIn(selectedDeviceIndex);
midiIn.MessageReceived += midiIn_MessageReceived;
midiIn.SysexMessageReceived += midiIn_SysexMessageReceived;
((MidiIn)midiIn).ErrorReceived += midiIn_ErrorReceived; // legacy-only event for malformed messages
midiIn.Start();
```

### Handling messages

Both backends deliver short messages as `MidiInMessageEventArgs`, which exposes a parsed `MidiEvent`, the original 32-bit `RawMessage`, and a `TimeSpan` `Timestamp` measured from when the port was opened:

```c#
void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
{
    Console.WriteLine($"Time {e.Timestamp:c} Message 0x{e.RawMessage:X8} Event {e.MidiEvent}");
}
```

> **Note on timestamps:** the WinRT backend preserves the underlying 100 ns resolution. The winmm backend reports millisecond resolution because `winmm.dll` only delivers milliseconds in its callback.
>
> **Threading:** both backends raise `MessageReceived` on a non-UI thread. If you need to update WinForms / WPF controls in the handler, marshal back to the UI thread (e.g. `Control.Invoke`, `Dispatcher.Invoke`).

To stop monitoring, call `Stop` and then `Dispose`:

```c#
midiIn.Stop();
midiIn.Dispose();
```

## Sending MIDI events

Once a device is open, the same code works for either backend:

```c#
var noteOnEvent = new NoteOnEvent(0, channel: 1, noteNumber: 60, velocity: 100, duration: 50);
midiOut.Send(noteOnEvent); // extension method — calls GetAsShortMessage internally
```

`Send(MidiEvent)` is an extension method on `IMidiOutput` that handles short MIDI messages. For sysex, use `SendBuffer` (see below).

Opening the device:

```c#
// WinRT:
IMidiOutput midiOut = await WinRTMidiOut.CreateAsync(outDevices[selectedIndex].Id);

// Legacy:
IMidiOutput midiOut = new MidiOut(comboBoxMidiOutDevices.SelectedIndex);
```

When finished:

```c#
midiOut.Dispose();
```

## Sending and Receiving Sysex Messages

### Sending

Sysex is sent via `SendBuffer` and works the same on both backends:

```c#
byte[] message = { 0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7 };
midiOut.SendBuffer(message);
```

On the winmm backend it's safe to break a long sysex message across multiple `SendBuffer` calls *as long as the calls are not asynchronously interleaved*. The WinRT backend expects the framing bytes (`0xF0` … `0xF7`) in a single buffer.

### Receiving

Subscribe to `SysexMessageReceived`. Both backends automatically allocate any receive buffers they need — the legacy winmm backend allocates 4 × 4 KB buffers internally when `Start()` is called:

```c#
midiIn.SysexMessageReceived += (s, e) =>
{
    byte[] sysexMessage = e.SysexBytes;
    Console.WriteLine($"Sysex {sysexMessage.Length} bytes at {e.Timestamp:c}");
};
```

## Converting between NAudio and WinRT MIDI types

`MidiMessageConverter` (in `NAudio.Wasapi`) provides bidirectional conversion between NAudio `MidiEvent` types and the WinRT `IMidiMessage` types from `Windows.Devices.Midi`. This is useful if you want to drop down to `Windows.Devices.Midi` directly (e.g. to use a WinRT-only feature) while keeping the rest of your code in NAudio:

```c#
// NAudio event → WinRT message
var noteOn = new NoteOnEvent(0, 1, 60, 100, 50);
IMidiMessage winRtMessage = MidiMessageConverter.ToWinRTMessage(noteOn.GetAsShortMessage());

// WinRT message → NAudio event
MidiEvent midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);
```
