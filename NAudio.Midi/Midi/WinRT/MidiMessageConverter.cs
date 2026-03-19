#if WINRT_MIDI
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Midi;
using Windows.Storage.Streams;

namespace NAudio.Midi
{
    /// <summary>
    /// Converts between NAudio MIDI types and Windows.Devices.Midi types
    /// </summary>
    public static class MidiMessageConverter
    {
        /// <summary>
        /// Converts a WinRT MIDI message to an NAudio MidiEvent
        /// </summary>
        /// <param name="message">The WinRT MIDI message</param>
        /// <returns>An NAudio MidiEvent, or null if the message type is not supported</returns>
        public static MidiEvent ToMidiEvent(IMidiMessage message)
        {
            switch (message.Type)
            {
                case MidiMessageType.NoteOn:
                    var noteOn = (MidiNoteOnMessage)message;
                    return new NoteOnEvent(0, noteOn.Channel + 1, noteOn.Note, noteOn.Velocity, 0);

                case MidiMessageType.NoteOff:
                    var noteOff = (MidiNoteOffMessage)message;
                    return new NoteEvent(0, noteOff.Channel + 1, MidiCommandCode.NoteOff, noteOff.Note, noteOff.Velocity);

                case MidiMessageType.ControlChange:
                    var cc = (MidiControlChangeMessage)message;
                    return new ControlChangeEvent(0, cc.Channel + 1, (MidiController)cc.Controller, cc.ControlValue);

                case MidiMessageType.ProgramChange:
                    var pc = (MidiProgramChangeMessage)message;
                    return new PatchChangeEvent(0, pc.Channel + 1, pc.Program);

                case MidiMessageType.ChannelPressure:
                    var cp = (MidiChannelPressureMessage)message;
                    return new ChannelAfterTouchEvent(0, cp.Channel + 1, cp.Pressure);

                case MidiMessageType.PitchBendChange:
                    var pb = (MidiPitchBendChangeMessage)message;
                    return new PitchWheelChangeEvent(0, pb.Channel + 1, pb.Bend);

                case MidiMessageType.PolyphonicKeyPressure:
                    var poly = (MidiPolyphonicKeyPressureMessage)message;
                    return new NoteEvent(0, poly.Channel + 1, MidiCommandCode.KeyAfterTouch, poly.Note, poly.Pressure);

                case MidiMessageType.SystemExclusive:
                    // Sysex is handled separately via SysexMessageReceived
                    return null;

                default:
                    // For system real-time messages, build from raw bytes
                    byte[] raw = message.RawData.ToArray();
                    if (raw.Length > 0)
                    {
                        int packed = raw[0];
                        if (raw.Length > 1) packed |= raw[1] << 8;
                        if (raw.Length > 2) packed |= raw[2] << 16;
                        try
                        {
                            return MidiEvent.FromRawMessage(packed);
                        }
                        catch (FormatException)
                        {
                            return null;
                        }
                    }
                    return null;
            }
        }

        /// <summary>
        /// Converts a packed NAudio short message to a WinRT MIDI message
        /// </summary>
        /// <param name="shortMessage">The packed MIDI message from MidiEvent.GetAsShortMessage()</param>
        /// <returns>A WinRT IMidiMessage</returns>
        public static IMidiMessage ToWinRTMessage(int shortMessage)
        {
            byte status = (byte)(shortMessage & 0xFF);
            byte data1 = (byte)((shortMessage >> 8) & 0xFF);
            byte data2 = (byte)((shortMessage >> 16) & 0xFF);
            byte channel = (byte)(status & 0x0F);
            byte command = (byte)(status & 0xF0);

            switch (command)
            {
                case 0x90:
                    return new MidiNoteOnMessage(channel, data1, data2);
                case 0x80:
                    return new MidiNoteOffMessage(channel, data1, data2);
                case 0xB0:
                    return new MidiControlChangeMessage(channel, data1, data2);
                case 0xC0:
                    return new MidiProgramChangeMessage(channel, data1);
                case 0xD0:
                    return new MidiChannelPressureMessage(channel, data1);
                case 0xE0:
                    return new MidiPitchBendChangeMessage(channel, (ushort)(data1 | (data2 << 7)));
                case 0xA0:
                    return new MidiPolyphonicKeyPressureMessage(channel, data1, data2);
                default:
                    // System messages (0xF0+) — handle specific ones
                    switch (status)
                    {
                        case 0xF8:
                            return new MidiTimingClockMessage();
                        case 0xFA:
                            return new MidiStartMessage();
                        case 0xFB:
                            return new MidiContinueMessage();
                        case 0xFC:
                            return new MidiStopMessage();
                        case 0xFE:
                            return new MidiActiveSensingMessage();
                        case 0xFF:
                            return new MidiSystemResetMessage();
                        default:
                            throw new ArgumentException($"Unsupported MIDI message: 0x{status:X2}", nameof(shortMessage));
                    }
            }
        }

        /// <summary>
        /// Converts a sysex byte array to a WinRT MidiSystemExclusiveMessage
        /// </summary>
        /// <param name="sysexData">The sysex bytes (including F0 and F7 framing)</param>
        /// <returns>A WinRT MidiSystemExclusiveMessage</returns>
        public static MidiSystemExclusiveMessage ToWinRTSysexMessage(byte[] sysexData)
        {
            var buffer = sysexData.AsBuffer();
            return new MidiSystemExclusiveMessage(buffer);
        }

        /// <summary>
        /// Converts a WinRT MIDI message to a packed short message integer
        /// </summary>
        /// <param name="message">The WinRT MIDI message</param>
        /// <returns>A packed 32-bit MIDI message</returns>
        public static int ToRawMessage(IMidiMessage message)
        {
            byte[] raw = message.RawData.ToArray();
            int packed = raw[0];
            if (raw.Length > 1) packed |= raw[1] << 8;
            if (raw.Length > 2) packed |= raw[2] << 16;
            return packed;
        }
    }
}
#endif
