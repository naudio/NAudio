using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Midi;
using System.IO;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]    
    public class PitchWheelChangeEventTests
    {
        [Test]
        public void GetAsShortMessageReturnsCorrectValue()
        {
            int channel = 2;
            int pitch = 0x3FFF; // 0x2000 is the default
            PitchWheelChangeEvent p = new PitchWheelChangeEvent(0, channel, pitch);

            Assert.That(p.GetAsShortMessage(), Is.EqualTo(0x007F7FE1));
        }

        [Test]
        public void ExportsCorrectValue()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            int channel = 2;
            int pitch = 0x207D; // 0x2000 is the default
            PitchWheelChangeEvent p = new PitchWheelChangeEvent(0, channel, pitch);
            
            long time = 0;
            p.Export(ref time, writer);

            Assert.That(ms.Length, Is.EqualTo(4));
            byte[] b = ms.GetBuffer();
            Assert.That(b[0], Is.EqualTo(0x0)); // event time
            Assert.That(b[1], Is.EqualTo(0xE1));
            Assert.That(b[2], Is.EqualTo(0x7D));
            Assert.That(b[3], Is.EqualTo(0x40));
        }
    }
}
