using System;
using System.Runtime.InteropServices.WindowsRuntime;
using NAudio.Midi;
using NUnit.Framework;
using Windows.Devices.Midi;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class MidiMessageConverterTests
    {
        // --- ToMidiEvent tests (WinRT → NAudio) ---

        [TestCase(0, 60, 100)]
        [TestCase(9, 36, 127)]
        [TestCase(15, 0, 1)]
        public void ToMidiEvent_NoteOn(byte channel, byte note, byte velocity)
        {
            var winRtMessage = new MidiNoteOnMessage(channel, note, velocity);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<NoteOnEvent>());
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.NoteOn));
            var noteEvent = (NoteEvent)midiEvent;
            Assert.That(noteEvent.NoteNumber, Is.EqualTo(note));
            Assert.That(noteEvent.Velocity, Is.EqualTo(velocity));
        }

        [TestCase(0, 60, 64)]
        [TestCase(5, 127, 0)]
        public void ToMidiEvent_NoteOff(byte channel, byte note, byte velocity)
        {
            var winRtMessage = new MidiNoteOffMessage(channel, note, velocity);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<NoteEvent>());
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.NoteOff));
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            var noteEvent = (NoteEvent)midiEvent;
            Assert.That(noteEvent.NoteNumber, Is.EqualTo(note));
            Assert.That(noteEvent.Velocity, Is.EqualTo(velocity));
        }

        [TestCase(0, 7, 100)]  // Volume
        [TestCase(0, 1, 64)]   // Modulation
        [TestCase(15, 64, 127)] // Sustain
        public void ToMidiEvent_ControlChange(byte channel, byte controller, byte value)
        {
            var winRtMessage = new MidiControlChangeMessage(channel, controller, value);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<ControlChangeEvent>());
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            var cc = (ControlChangeEvent)midiEvent;
            Assert.That((int)cc.Controller, Is.EqualTo(controller));
            Assert.That(cc.ControllerValue, Is.EqualTo(value));
        }

        [TestCase(0, 0)]
        [TestCase(9, 42)]
        [TestCase(15, 127)]
        public void ToMidiEvent_ProgramChange(byte channel, byte program)
        {
            var winRtMessage = new MidiProgramChangeMessage(channel, program);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<PatchChangeEvent>());
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            var pc = (PatchChangeEvent)midiEvent;
            Assert.That(pc.Patch, Is.EqualTo(program));
        }

        [TestCase(0, 0)]
        [TestCase(3, 64)]
        [TestCase(15, 127)]
        public void ToMidiEvent_ChannelPressure(byte channel, byte pressure)
        {
            var winRtMessage = new MidiChannelPressureMessage(channel, pressure);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<ChannelAfterTouchEvent>());
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            var afterTouch = (ChannelAfterTouchEvent)midiEvent;
            Assert.That(afterTouch.AfterTouchPressure, Is.EqualTo(pressure));
        }

        [TestCase(0, 8192)]   // Center
        [TestCase(0, 0)]      // Min
        [TestCase(0, 16383)]  // Max
        [TestCase(7, 4096)]
        public void ToMidiEvent_PitchBend(byte channel, int bend)
        {
            var winRtMessage = new MidiPitchBendChangeMessage(channel, (ushort)bend);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<PitchWheelChangeEvent>());
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            var pb = (PitchWheelChangeEvent)midiEvent;
            Assert.That(pb.Pitch, Is.EqualTo(bend));
        }

        [TestCase(0, 60, 80)]
        [TestCase(5, 48, 127)]
        public void ToMidiEvent_PolyphonicKeyPressure(byte channel, byte note, byte pressure)
        {
            var winRtMessage = new MidiPolyphonicKeyPressureMessage(channel, note, pressure);
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.TypeOf<NoteEvent>());
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.KeyAfterTouch));
            Assert.That(midiEvent.Channel, Is.EqualTo(channel + 1));
            var noteEvent = (NoteEvent)midiEvent;
            Assert.That(noteEvent.NoteNumber, Is.EqualTo(note));
            Assert.That(noteEvent.Velocity, Is.EqualTo(pressure));
        }

        [Test]
        public void ToMidiEvent_SystemExclusive_ReturnsNull()
        {
            var sysexData = new byte[] { 0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7 };
            var winRtMessage = new MidiSystemExclusiveMessage(sysexData.AsBuffer());
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.Null);
        }

        [Test]
        public void ToMidiEvent_TimingClock()
        {
            var winRtMessage = new MidiTimingClockMessage();
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.Not.Null);
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.TimingClock));
        }

        [Test]
        public void ToMidiEvent_ActiveSensing()
        {
            var winRtMessage = new MidiActiveSensingMessage();
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.Not.Null);
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.AutoSensing));
        }

        [Test]
        public void ToMidiEvent_StartMessage()
        {
            var winRtMessage = new MidiStartMessage();
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.Not.Null);
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.StartSequence));
        }

        [Test]
        public void ToMidiEvent_ContinueMessage()
        {
            var winRtMessage = new MidiContinueMessage();
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.Not.Null);
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.ContinueSequence));
        }

        [Test]
        public void ToMidiEvent_StopMessage()
        {
            var winRtMessage = new MidiStopMessage();
            var midiEvent = MidiMessageConverter.ToMidiEvent(winRtMessage);

            Assert.That(midiEvent, Is.Not.Null);
            Assert.That(midiEvent.CommandCode, Is.EqualTo(MidiCommandCode.StopSequence));
        }

        // --- ToWinRTMessage tests (NAudio → WinRT) ---

        [TestCase(1, 60, 100)]
        [TestCase(10, 36, 127)]
        [TestCase(16, 0, 1)]
        public void ToWinRTMessage_NoteOn(int channel, int note, int velocity)
        {
            var noteOn = new NoteOnEvent(0, channel, note, velocity, 0);
            var result = MidiMessageConverter.ToWinRTMessage(noteOn.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiNoteOnMessage>());
            var msg = (MidiNoteOnMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Note, Is.EqualTo(note));
            Assert.That(msg.Velocity, Is.EqualTo(velocity));
        }

        [TestCase(1, 60, 64)]
        [TestCase(16, 127, 0)]
        public void ToWinRTMessage_NoteOff(int channel, int note, int velocity)
        {
            var noteOff = new NoteEvent(0, channel, MidiCommandCode.NoteOff, note, velocity);
            var result = MidiMessageConverter.ToWinRTMessage(noteOff.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiNoteOffMessage>());
            var msg = (MidiNoteOffMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Note, Is.EqualTo(note));
            Assert.That(msg.Velocity, Is.EqualTo(velocity));
        }

        [TestCase(1, MidiController.MainVolume, 100)]
        [TestCase(1, MidiController.Modulation, 64)]
        [TestCase(16, MidiController.Sustain, 127)]
        public void ToWinRTMessage_ControlChange(int channel, MidiController controller, int value)
        {
            var cc = new ControlChangeEvent(0, channel, controller, value);
            var result = MidiMessageConverter.ToWinRTMessage(cc.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiControlChangeMessage>());
            var msg = (MidiControlChangeMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Controller, Is.EqualTo((byte)controller));
            Assert.That(msg.ControlValue, Is.EqualTo(value));
        }

        [TestCase(1, 0)]
        [TestCase(10, 42)]
        [TestCase(16, 127)]
        public void ToWinRTMessage_ProgramChange(int channel, int program)
        {
            var pc = new PatchChangeEvent(0, channel, program);
            var result = MidiMessageConverter.ToWinRTMessage(pc.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiProgramChangeMessage>());
            var msg = (MidiProgramChangeMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Program, Is.EqualTo(program));
        }

        [TestCase(1, 0)]
        [TestCase(4, 64)]
        [TestCase(16, 127)]
        public void ToWinRTMessage_ChannelPressure(int channel, int pressure)
        {
            var cp = new ChannelAfterTouchEvent(0, channel, pressure);
            var result = MidiMessageConverter.ToWinRTMessage(cp.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiChannelPressureMessage>());
            var msg = (MidiChannelPressureMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Pressure, Is.EqualTo(pressure));
        }

        [TestCase(1, 8192)]   // Center
        [TestCase(1, 0)]      // Min
        [TestCase(1, 16383)]  // Max
        [TestCase(8, 4096)]
        public void ToWinRTMessage_PitchBend(int channel, int pitch)
        {
            var pb = new PitchWheelChangeEvent(0, channel, pitch);
            var result = MidiMessageConverter.ToWinRTMessage(pb.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiPitchBendChangeMessage>());
            var msg = (MidiPitchBendChangeMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Bend, Is.EqualTo(pitch));
        }

        [TestCase(1, 60, 80)]
        [TestCase(6, 48, 127)]
        public void ToWinRTMessage_PolyphonicKeyPressure(int channel, int note, int pressure)
        {
            var poly = new NoteEvent(0, channel, MidiCommandCode.KeyAfterTouch, note, pressure);
            var result = MidiMessageConverter.ToWinRTMessage(poly.GetAsShortMessage());

            Assert.That(result, Is.TypeOf<MidiPolyphonicKeyPressureMessage>());
            var msg = (MidiPolyphonicKeyPressureMessage)result;
            Assert.That(msg.Channel, Is.EqualTo(channel - 1));
            Assert.That(msg.Note, Is.EqualTo(note));
            Assert.That(msg.Pressure, Is.EqualTo(pressure));
        }

        [Test]
        public void ToWinRTMessage_TimingClock()
        {
            var midiEvent = new MidiEvent(0, 1, MidiCommandCode.TimingClock);
            var result = MidiMessageConverter.ToWinRTMessage(midiEvent.GetAsShortMessage());
            Assert.That(result, Is.TypeOf<MidiTimingClockMessage>());
        }

        [Test]
        public void ToWinRTMessage_StartSequence()
        {
            var midiEvent = new MidiEvent(0, 1, MidiCommandCode.StartSequence);
            var result = MidiMessageConverter.ToWinRTMessage(midiEvent.GetAsShortMessage());
            Assert.That(result, Is.TypeOf<MidiStartMessage>());
        }

        [Test]
        public void ToWinRTMessage_ContinueSequence()
        {
            var midiEvent = new MidiEvent(0, 1, MidiCommandCode.ContinueSequence);
            var result = MidiMessageConverter.ToWinRTMessage(midiEvent.GetAsShortMessage());
            Assert.That(result, Is.TypeOf<MidiContinueMessage>());
        }

        [Test]
        public void ToWinRTMessage_StopSequence()
        {
            var midiEvent = new MidiEvent(0, 1, MidiCommandCode.StopSequence);
            var result = MidiMessageConverter.ToWinRTMessage(midiEvent.GetAsShortMessage());
            Assert.That(result, Is.TypeOf<MidiStopMessage>());
        }

        [Test]
        public void ToWinRTMessage_ActiveSensing()
        {
            var midiEvent = new MidiEvent(0, 1, MidiCommandCode.AutoSensing);
            var result = MidiMessageConverter.ToWinRTMessage(midiEvent.GetAsShortMessage());
            Assert.That(result, Is.TypeOf<MidiActiveSensingMessage>());
        }

        [Test]
        public void ToWinRTMessage_UnsupportedThrows()
        {
            // 0xF1 = MTC Quarter Frame, not in our switch
            int unsupportedMessage = 0xF1;
            Assert.Throws<ArgumentException>(() => MidiMessageConverter.ToWinRTMessage(unsupportedMessage));
        }

        // --- ToWinRTSysexMessage tests ---

        [Test]
        public void ToWinRTSysexMessage_CreatesValidMessage()
        {
            var sysexData = new byte[] { 0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7 };
            var result = MidiMessageConverter.ToWinRTSysexMessage(sysexData);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Type, Is.EqualTo(MidiMessageType.SystemExclusive));
            var raw = result.RawData.ToArray();
            Assert.That(raw, Is.EqualTo(sysexData));
        }

        // --- ToRawMessage tests ---

        [Test]
        public void ToRawMessage_NoteOn_PacksCorrectly()
        {
            var msg = new MidiNoteOnMessage(0, 60, 100);
            int raw = MidiMessageConverter.ToRawMessage(msg);

            Assert.That(raw & 0xFF, Is.EqualTo(0x90));       // status: note on, channel 0
            Assert.That((raw >> 8) & 0xFF, Is.EqualTo(60));   // note
            Assert.That((raw >> 16) & 0xFF, Is.EqualTo(100)); // velocity
        }

        [Test]
        public void ToRawMessage_ProgramChange_PacksCorrectly()
        {
            var msg = new MidiProgramChangeMessage(3, 42);
            int raw = MidiMessageConverter.ToRawMessage(msg);

            Assert.That(raw & 0xFF, Is.EqualTo(0xC3));      // status: program change, channel 3
            Assert.That((raw >> 8) & 0xFF, Is.EqualTo(42));  // program
        }

        // --- Round-trip tests (NAudio → WinRT → NAudio) ---

        [TestCase(1, 60, 100)]
        [TestCase(10, 36, 127)]
        [TestCase(16, 127, 1)]
        public void RoundTrip_NoteOn(int channel, int note, int velocity)
        {
            var original = new NoteOnEvent(0, channel, note, velocity, 0);
            int shortMsg = original.GetAsShortMessage();

            var winRtMsg = MidiMessageConverter.ToWinRTMessage(shortMsg);
            var roundTripped = MidiMessageConverter.ToMidiEvent(winRtMsg);

            Assert.That(roundTripped, Is.TypeOf<NoteOnEvent>());
            Assert.That(roundTripped.Channel, Is.EqualTo(channel));
            var noteEvent = (NoteEvent)roundTripped;
            Assert.That(noteEvent.NoteNumber, Is.EqualTo(note));
            Assert.That(noteEvent.Velocity, Is.EqualTo(velocity));
        }

        [TestCase(1, MidiController.MainVolume, 100)]
        [TestCase(16, MidiController.Pan, 64)]
        public void RoundTrip_ControlChange(int channel, MidiController controller, int value)
        {
            var original = new ControlChangeEvent(0, channel, controller, value);
            int shortMsg = original.GetAsShortMessage();

            var winRtMsg = MidiMessageConverter.ToWinRTMessage(shortMsg);
            var roundTripped = MidiMessageConverter.ToMidiEvent(winRtMsg);

            Assert.That(roundTripped, Is.TypeOf<ControlChangeEvent>());
            Assert.That(roundTripped.Channel, Is.EqualTo(channel));
            var cc = (ControlChangeEvent)roundTripped;
            Assert.That(cc.Controller, Is.EqualTo(controller));
            Assert.That(cc.ControllerValue, Is.EqualTo(value));
        }

        [TestCase(1, 0)]
        [TestCase(1, 8192)]
        [TestCase(1, 16383)]
        [TestCase(8, 4096)]
        public void RoundTrip_PitchBend(int channel, int pitch)
        {
            var original = new PitchWheelChangeEvent(0, channel, pitch);
            int shortMsg = original.GetAsShortMessage();

            var winRtMsg = MidiMessageConverter.ToWinRTMessage(shortMsg);
            var roundTripped = MidiMessageConverter.ToMidiEvent(winRtMsg);

            Assert.That(roundTripped, Is.TypeOf<PitchWheelChangeEvent>());
            Assert.That(roundTripped.Channel, Is.EqualTo(channel));
            var pb = (PitchWheelChangeEvent)roundTripped;
            Assert.That(pb.Pitch, Is.EqualTo(pitch));
        }

        [TestCase(1, 42)]
        [TestCase(10, 0)]
        [TestCase(16, 127)]
        public void RoundTrip_ProgramChange(int channel, int program)
        {
            var original = new PatchChangeEvent(0, channel, program);
            int shortMsg = original.GetAsShortMessage();

            var winRtMsg = MidiMessageConverter.ToWinRTMessage(shortMsg);
            var roundTripped = MidiMessageConverter.ToMidiEvent(winRtMsg);

            Assert.That(roundTripped, Is.TypeOf<PatchChangeEvent>());
            Assert.That(roundTripped.Channel, Is.EqualTo(channel));
            var pc = (PatchChangeEvent)roundTripped;
            Assert.That(pc.Patch, Is.EqualTo(program));
        }

        [TestCase(1, 64)]
        [TestCase(16, 0)]
        [TestCase(4, 127)]
        public void RoundTrip_ChannelPressure(int channel, int pressure)
        {
            var original = new ChannelAfterTouchEvent(0, channel, pressure);
            int shortMsg = original.GetAsShortMessage();

            var winRtMsg = MidiMessageConverter.ToWinRTMessage(shortMsg);
            var roundTripped = MidiMessageConverter.ToMidiEvent(winRtMsg);

            Assert.That(roundTripped, Is.TypeOf<ChannelAfterTouchEvent>());
            Assert.That(roundTripped.Channel, Is.EqualTo(channel));
            var cp = (ChannelAfterTouchEvent)roundTripped;
            Assert.That(cp.AfterTouchPressure, Is.EqualTo(pressure));
        }

        [Test]
        public void RoundTrip_Sysex()
        {
            var sysexData = new byte[] { 0xF0, 0x00, 0x21, 0x1D, 0x01, 0x01, 0xF7 };
            var winRtMsg = MidiMessageConverter.ToWinRTSysexMessage(sysexData);
            var raw = winRtMsg.RawData.ToArray();
            Assert.That(raw, Is.EqualTo(sysexData));
        }
    }
}
