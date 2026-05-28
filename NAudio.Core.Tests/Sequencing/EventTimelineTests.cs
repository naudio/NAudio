using System.Linq;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class EventTimelineTests
    {
        [Test]
        public void Empty_Range_Returns_Empty()
        {
            var t = new EventTimeline<int>();
            Assert.That(t.EventsInRange(0, 1000).Count, Is.EqualTo(0));
        }

        [Test]
        public void Returns_Events_In_Range()
        {
            var t = new EventTimeline<int>();
            t.Add(0, 1);
            t.Add(100, 2);
            t.Add(200, 3);
            var events = t.EventsInRange(50, 250).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Range_Is_Half_Open()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(200, 2);
            var events = t.EventsInRange(100, 200).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Out_Of_Order_Inserts_Are_Sorted()
        {
            var t = new EventTimeline<int>();
            t.Add(300, 3);
            t.Add(100, 1);
            t.Add(200, 2);
            var events = t.EventsInRange(0, 1000).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Same_Tick_Preserves_Insertion_Order()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(100, 2);
            t.Add(100, 3);
            var events = t.EventsInRange(0, 1000).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Clear_Removes_All()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(200, 2);
            t.Clear();
            Assert.That(t.Count, Is.EqualTo(0));
        }

        [Test]
        public void FirstTick_LastTick_Reported()
        {
            var t = new EventTimeline<int>();
            Assert.That(t.FirstTick, Is.Null);
            Assert.That(t.LastTick, Is.Null);
            t.Add(100, 1);
            t.Add(50, 2);
            t.Add(200, 3);
            Assert.That(t.FirstTick, Is.EqualTo(50));
            Assert.That(t.LastTick, Is.EqualTo(200));
        }
    }
}
