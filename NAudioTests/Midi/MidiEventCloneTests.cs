using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using NUnit.Framework;

namespace NAudioTests.Midi
{
    [TestFixture]
    public class MidiEventCloneTests
    {
        [Test]
        public void CanCloneForSameTrack()
        {
            var collection = new MidiEventCollection(0, 120);
            collection.AddEvent(new NoteOnEvent(0, 1, 30, 100, 15), 0);
            
            var clone = (NoteOnEvent)collection[0][0].Clone();
            clone.AbsoluteTime += 15;
            clone.NoteNumber++;
            collection.AddEvent(clone, 0);

            collection.PrepareForExport();

            Assert.That(collection[0][0].AbsoluteTime, Is.EqualTo(0));
            Assert.That(collection[0][1].AbsoluteTime, Is.EqualTo(15));
            Assert.That(((NoteOnEvent)collection[0][0]).NoteNumber, Is.EqualTo(30));
            Assert.That(((NoteOnEvent)collection[0][1]).NoteNumber, Is.EqualTo(31));
        }

        [Test]
        public void NoteOnIsDeepClone()
        {
            var ev = new NoteOnEvent(0, 1, 30, 100, 15);
            var clone = (NoteOnEvent)ev.Clone();
            Assert.That(clone.OffEvent, Is.Not.SameAs(ev.OffEvent));
        }

        [Test]
        public void SequencerSpecificIsDeepClone()
        {
            var ev = new SequencerSpecificEvent(new byte[] { 0x01 }, 0);
            var clone = (SequencerSpecificEvent)ev.Clone();
            Assert.That(clone.Data, Is.Not.SameAs(ev.Data));
        }

        private static IEnumerable<TestCaseData> AllMidiEventTypes =>
            from midiEventType in typeof(MidiEvent).Assembly.GetTypes().Where(typeof(MidiEvent).IsAssignableFrom)
            select new TestCaseData(midiEventType).SetName(midiEventType.Name);

        private static readonly Dictionary<Type, MidiEvent> TestMidiEvents = new[]
        {
            new MidiEvent(0, 1, MidiCommandCode.Eox),
            new ChannelAfterTouchEvent(0, 1, 0),
            new ControlChangeEvent(0, 1, MidiController.AllNotesOff, 0),
            new KeySignatureEvent(0, 0, 0),
            new MetaEvent(MetaEventType.Copyright, 0, 0),
            new RawMetaEvent(MetaEventType.Copyright, 0, new byte[0]),
            new NoteEvent(0, 1, MidiCommandCode.NoteOff, 0, 0), 
            new NoteOnEvent(0, 1, 0, 0, 0),
            new PatchChangeEvent(0, 1, 0),
            new PitchWheelChangeEvent(0, 1, 0),
            new SequencerSpecificEvent(new byte[0], 0),
            new SmpteOffsetEvent(1, 1, 1, 1, 1),
            new SysexEvent(),
            new TempoEvent(0, 0),
            new TextEvent(string.Empty, MetaEventType.Copyright, 0),
            new TimeSignatureEvent(0, 1, 1, 1, 1),
            new TrackSequenceNumberEvent(1)
        }.ToDictionary(_ => _.GetType());

        [Test, TestCaseSource(nameof(AllMidiEventTypes))]
        public void CloneReturnsCorrectType(Type midiEventType)
        {
            MidiEvent instance;
            Assert.That(TestMidiEvents.TryGetValue(midiEventType, out instance), $"{midiEventType.Name} should be tested.");
            Assert.That(instance.Clone(), Is.TypeOf(midiEventType));
        }
    }
}
