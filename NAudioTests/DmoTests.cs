using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Dmo;

namespace NAudioTests
{
    [TestFixture]
    public class DmoTests
    {
        [Test]
        public void CanEnumerateAudioEffects()
        {
            Console.WriteLine("Audio Effects:");
            foreach (string name in DmoEnumerator.GetAudioEffectNames())
            {
                Console.WriteLine(name);
            }
        }

        [Test]
        public void CanEnumerateAudioEncoders()
        {
            Console.WriteLine("Audio Encoders:");
            foreach (string name in DmoEnumerator.GetAudioEncoderNames())
            {
                Console.WriteLine(name);
            }
        }

        [Test]
        public void CanEnumerateAudioDecoders()
        {
            Console.WriteLine("Audio Decoders:");
            foreach (string name in DmoEnumerator.GetAudioDecoderNames())
            {
                Console.WriteLine(name);
            }
        }

    }
}
