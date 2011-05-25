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
        [Category("IntegrationTest")]
        public void CanEnumerateAudioEffects()
        {
            Debug.WriteLine("Audio Effects:");
            foreach (var dmo in DmoEnumerator.GetAudioEffectNames())
            {
                Debug.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
                var mediaObject = Activator.CreateInstance(Type.GetTypeFromCLSID(dmo.Clsid));
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateAudioEncoders()
        {
            Debug.WriteLine("Audio Encoders:");
            foreach (var dmo in DmoEnumerator.GetAudioEncoderNames())
            {
                Debug.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateAudioDecoders()
        {
            Debug.WriteLine("Audio Decoders:");
            foreach (var dmo in DmoEnumerator.GetAudioDecoderNames())
            {
                Debug.WriteLine(string.Format("{0} {1}", dmo.Name, dmo.Clsid));
            }
        }
    }
}

