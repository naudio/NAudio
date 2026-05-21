using NAudio.Wave.Asio;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioDriverCapabilityTests
    {
        [Test]
        public void AllInputChannels_ReturnsContiguousRange()
        {
            var capability = new AsioDriverCapability { NbInputChannels = 4 };

            Assert.That(capability.AllInputChannels, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void AllOutputChannels_ReturnsContiguousRange()
        {
            var capability = new AsioDriverCapability { NbOutputChannels = 8 };

            Assert.That(capability.AllOutputChannels, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
        }

        [Test]
        public void AllInputChannels_ReturnsEmptyArray_WhenNoInputs()
        {
            var capability = new AsioDriverCapability { NbInputChannels = 0 };

            Assert.That(capability.AllInputChannels, Is.Empty);
        }

        [Test]
        public void AllInputChannels_ReturnsFreshArrayEachCall()
        {
            var capability = new AsioDriverCapability { NbInputChannels = 2 };

            var first = capability.AllInputChannels;
            first[0] = 99;

            Assert.That(capability.AllInputChannels[0], Is.EqualTo(0),
                "AllInputChannels must return a fresh array each call so mutation by the caller does not affect internal state.");
        }
    }
}
