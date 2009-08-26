using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Dmo;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioTests.Dmo
{
    [TestFixture]
    public class DmoTests
    {
        [Test]
        public void CanEnumerateAudioEffects()
        {
            Console.WriteLine("Audio Effects:");
            foreach (var dmo in DmoEnumerator.GetAudioEffectNames())
            {
                Console.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
                var mediaObject = Activator.CreateInstance(Type.GetTypeFromCLSID(dmo.Clsid));

            }
        }

        [Test]
        public void CanEnumerateAudioEncoders()
        {
            Console.WriteLine("Audio Encoders:");
            foreach (var dmo in DmoEnumerator.GetAudioEncoderNames())
            {
                Console.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
            }
        }

        [Test]
        public void CanEnumerateAudioDecoders()
        {
            Console.WriteLine("Audio Decoders:");
            foreach (var dmo in DmoEnumerator.GetAudioDecoderNames())
            {
                Console.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
            }
        }




    }
}

