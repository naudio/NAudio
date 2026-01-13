using NAudio.Mixer;
using NAudio.Wave;
using NUnit.Framework;
using System.Diagnostics;

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
                Debug.WriteLine($"DESTINATION: {destination.Name} {destination.TypeDescription} (Type: {destination.ComponentType}, Target: {destination.TargetName})");

                if (destination.ComponentType == MixerLineComponentType.DestinationWaveIn)
                {
                    foreach (MixerLine source in destination.Sources)
                    {
                        Debug.WriteLine($"{source.Name} {source.TypeDescription} (Source: {source.IsSource}, Target: {source.TargetName})");
                        if (source.ComponentType == MixerLineComponentType.SourceMicrophone)
                        {
                            Debug.WriteLine($"Found the microphone: {source.Name}");
                            foreach (MixerControl control in source.Controls)
                            {
                                if (control.ControlType == MixerControlType.Volume)
                                {
                                    Debug.WriteLine($"Volume Found: {control}");
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
            Debug.WriteLine($"Device {deviceIndex}: {mixer.Name}");
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
            Debug.WriteLine($"Destination {destinationIndex}: {destination} ({destination.Channels})");
            foreach (MixerControl control in destination.Controls)
            {
                Debug.WriteLine($"CONTROL: {control}");
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
            Debug.WriteLine($"Source {sourceIndex}: {sourceLine}");
            foreach (MixerControl control in sourceLine.Controls)
            {
                Debug.WriteLine($"CONTROL: {control}");
            }
        }
    }
}
