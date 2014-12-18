using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Mixer;
using System.Diagnostics;
using NAudio;
using NAudio.Wave;

namespace NAudioTests
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class MixerApiTests
    {
        [Test]
        public void CanEnumerateAllMixerControls()
        {
            int devices = Mixer.NumberOfDevices;
            Assert.That(devices > 0, "Expected at least one mixer device");
            for (int device = 0; device < devices; device++)
            {
                ExploreMixerDevice(device);
                Debug.WriteLine("");
            }
        }

        [Test]
        public void CanFindDefaultWaveIn()
        {
            int defaultWaveInMixerId = MixerLine.GetMixerIdForWaveIn(0);
            Mixer mixer = new Mixer(defaultWaveInMixerId);
            foreach (MixerLine destination in mixer.Destinations)
            {
                Debug.WriteLine(String.Format("DESTINATION: {0} {1} (Type: {2}, Target: {3})",
                    destination.Name, destination.TypeDescription, destination.ComponentType, destination.TargetName));

                if (destination.ComponentType == MixerLineComponentType.DestinationWaveIn)
                {
                    foreach (MixerLine source in destination.Sources)
                    {
                        Debug.WriteLine(String.Format("{0} {1} (Source: {2}, Target: {3})",
                            source.Name, source.TypeDescription, source.IsSource, source.TargetName));
                        if (source.ComponentType == MixerLineComponentType.SourceMicrophone)
                        {
                            Debug.WriteLine(String.Format("Found the microphone: {0}", source.Name));
                            foreach (MixerControl control in source.Controls)
                            {
                                if (control.ControlType == MixerControlType.Volume)
                                {
                                    Debug.WriteLine(String.Format("Volume Found: {0}", control));
                                    UnsignedMixerControl umc = (UnsignedMixerControl)control;
                                    uint originalValue = umc.Value;
                                    umc.Value = umc.MinValue;
                                    Assert.AreEqual(umc.MinValue, umc.Value, "Set Minimum Correctly");
                                    umc.Value = umc.MaxValue;
                                    Assert.AreEqual(umc.MaxValue, umc.Value, "Set Maximum Correctly");
                                    umc.Value = umc.MaxValue / 2;
                                    Assert.AreEqual(umc.MaxValue / 2, umc.Value, "Set MidPoint Correctly");
                                    umc.Value = originalValue;
                                    Assert.AreEqual(originalValue, umc.Value, "Set Original Correctly");
                                }
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void CanGetWaveInMixerLine()
        {
            using (var waveIn = new WaveInEvent())
            {
                MixerLine line = waveIn.GetMixerLine();                
                //Debug.WriteLine(String.Format("Mic Level {0}", level));
            }
        }

        private static void ExploreMixerDevice(int deviceIndex)
        {
            Mixer mixer = new Mixer(deviceIndex);
            Debug.WriteLine(String.Format("Device {0}: {1}",deviceIndex,mixer.Name));
            Debug.WriteLine("--------------------------------------------");
            int destinations = mixer.DestinationCount;
            Assert.That(destinations > 0, "Expected at least one destination");
            for (int destinationIndex = 0; destinationIndex < destinations; destinationIndex++)
            {
                ExploreMixerDestination(mixer, destinationIndex);
            }
        }

        private static void ExploreMixerDestination(Mixer mixer, int destinationIndex)
        {
            var destination = mixer.GetDestination(destinationIndex);
            Debug.WriteLine(String.Format("Destination {0}: {1}", 
                destinationIndex, destination));
            int channels = destination.Channels;
            foreach (MixerControl control in destination.Controls)
            {
                Debug.WriteLine(String.Format("CONTROL: {0}", control));
            }
            int sources = destination.SourceCount;
            for (int sourceIndex = 0; sourceIndex < sources; sourceIndex++)
            {
                ExploreMixerSource(destination, sourceIndex);
            }
        }

        private static void ExploreMixerSource(MixerLine destinationLine, int sourceIndex)
        {
            var sourceLine = destinationLine.GetSource(sourceIndex);
            Debug.WriteLine(String.Format("Source {0}: {1}",
                sourceIndex, sourceLine));
            foreach (MixerControl control in sourceLine.Controls)
            {
                Debug.WriteLine(String.Format("CONTROL: {0}", control));
            }
        }
    }
}
