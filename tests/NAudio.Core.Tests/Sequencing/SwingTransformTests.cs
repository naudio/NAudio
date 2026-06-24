using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class SwingTransformTests
    {
        private static readonly long Grid = MusicalTime.TicksPerDivision(16); // 16th-note grid

        [Test]
        public void Zero_Amount_Is_Identity()
        {
            var t = new SwingTransform(Grid, 0.0);
            for (int i = 0; i < 16; i++)
            {
                long tick = i * Grid;
                Assert.That(t.Transform(tick), Is.EqualTo(tick));
            }
        }

        [Test]
        public void Even_Grid_Lines_Are_Untouched()
        {
            var t = new SwingTransform(Grid, 0.5);
            Assert.That(t.Transform(0), Is.EqualTo(0));
            Assert.That(t.Transform(Grid * 2), Is.EqualTo(Grid * 2));
            Assert.That(t.Transform(Grid * 4), Is.EqualTo(Grid * 4));
        }

        [Test]
        public void Odd_Grid_Lines_Are_Shifted_Forward()
        {
            var t = new SwingTransform(Grid, 0.5);
            // 0.5 * 16th = a 32nd offset.
            var shift = (long)(0.5 * Grid);
            Assert.That(t.Transform(Grid), Is.EqualTo(Grid + shift));
            Assert.That(t.Transform(Grid * 3), Is.EqualTo(Grid * 3 + shift));
            Assert.That(t.Transform(Grid * 5), Is.EqualTo(Grid * 5 + shift));
        }

        [Test]
        public void Off_Grid_Events_Are_Untouched()
        {
            var t = new SwingTransform(Grid, 0.5);
            // 5 ticks isn't on the 16th grid.
            Assert.That(t.Transform(5), Is.EqualTo(5));
            Assert.That(t.Transform(Grid + 5), Is.EqualTo(Grid + 5));
        }

        [Test]
        public void MaxShiftTicks_Bounds_The_Shift()
        {
            var t = new SwingTransform(Grid, 0.4);
            // The reported bound must be at least as large as any actual shift the transform applies.
            // Test across a span: every odd grid line shifted by amount*Grid.
            for (int i = 0; i < 32; i++)
            {
                long nominal = i * Grid;
                long effective = t.Transform(nominal);
                Assert.That(effective - nominal, Is.LessThanOrEqualTo(t.MaxShiftTicks),
                    $"Shift at i={i} exceeded bound");
            }
        }

        [Test]
        public void Negative_Amount_Clamps_To_Zero()
        {
            var t = new SwingTransform(Grid, -0.3);
            Assert.That(t.Amount, Is.EqualTo(0));
            Assert.That(t.Transform(Grid), Is.EqualTo(Grid));
        }
    }
}
