using System.Linq;
using NAudio.Effects;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class ParameterDispatchTests
    {
        private static EffectParameter Gain(GainEffect e)
            => e.Parameters.First(p => p.Name == "Gain");

        [Test]
        public void WriteIsDeferredUntilDrainWhenAttached()
        {
            var gain = new GainEffect();
            var queue = new ParameterDispatchQueue();
            queue.Attach(gain);

            Gain(gain).Value = -6f;

            // Not applied yet — the audio thread hasn't drained.
            Assert.That(gain.GainDb, Is.EqualTo(0f).Within(1e-3f));

            queue.Drain();

            Assert.That(gain.GainDb, Is.EqualTo(-6f).Within(1e-3f));
        }

        [Test]
        public void LatestValueWinsAfterDrain()
        {
            var gain = new GainEffect();
            var queue = new ParameterDispatchQueue();
            queue.Attach(gain);
            var p = Gain(gain);

            p.Value = -6f;
            p.Value = -12f;
            p.Value = -3f;
            queue.Drain();

            Assert.That(gain.GainDb, Is.EqualTo(-3f).Within(1e-3f));
        }

        [Test]
        public void ValueIsClampedBeforeItIsPosted()
        {
            var gain = new GainEffect();
            var queue = new ParameterDispatchQueue();
            queue.Attach(gain);
            var p = Gain(gain);

            p.Value = 999f;
            queue.Drain();
            Assert.That(gain.GainDb, Is.EqualTo(24f).Within(1e-3f));

            p.Value = -999f;
            queue.Drain();
            Assert.That(gain.GainDb, Is.EqualTo(-60f).Within(1e-3f));
        }

        [Test]
        public void DetachRestoresInlineApplication()
        {
            var gain = new GainEffect();
            var queue = new ParameterDispatchQueue();
            queue.Attach(gain);
            queue.Detach(gain);

            Gain(gain).Value = -6f;

            // No dispatch — applied immediately, no drain needed.
            Assert.That(gain.GainDb, Is.EqualTo(-6f).Within(1e-3f));
        }

        [Test]
        public void OverflowDropsWritesWithoutThrowing()
        {
            var gain = new GainEffect();
            var queue = new ParameterDispatchQueue(2); // tiny ring
            queue.Attach(gain);
            var p = Gain(gain);

            for (var i = 0; i < 1000; i++)
                p.Value = -6f;

            Assert.DoesNotThrow(() => queue.Drain());
            Assert.That(gain.GainDb, Is.EqualTo(-6f).Within(1e-3f));
        }

        [Test]
        public void ValueReflectsTheRequestedValueImmediatelyWhenAttached()
        {
            var gain = new GainEffect();
            var queue = new ParameterDispatchQueue();
            queue.Attach(gain);
            var p = Gain(gain);

            p.Value = -6f;
            // Optimistic read: the parameter reports the requested value before
            // the drain (so a two-way-bound UI control does not snap back), even
            // though the effect property has not changed yet.
            Assert.That(p.Value, Is.EqualTo(-6f).Within(1e-3f));
            Assert.That(gain.GainDb, Is.EqualTo(0f).Within(1e-3f));

            queue.Drain();
            Assert.That(p.Value, Is.EqualTo(-6f).Within(1e-3f));
            Assert.That(gain.GainDb, Is.EqualTo(-6f).Within(1e-3f));

            queue.Detach(gain);
            Assert.That(p.Value, Is.EqualTo(-6f).Within(1e-3f));
        }

        [Test]
        public void InlineWhenNoQueueIsAttached()
        {
            var gain = new GainEffect();
            Gain(gain).Value = -9f;
            Assert.That(gain.GainDb, Is.EqualTo(-9f).Within(1e-3f));
        }
    }
}
