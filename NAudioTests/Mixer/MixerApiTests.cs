using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Mixer;
using System.Diagnostics;
using NAudio;

namespace NAudioTests
{
    [TestFixture]
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
            MixerLine waveIn = MixerLine.ForWaveIn(0);
            Debug.WriteLine(String.Format("{0} {1} (Source: {2})", waveIn.Name, waveIn.TypeDescription, waveIn.IsSource));
            foreach (MixerLine source in waveIn.Sources)
            {
                Debug.WriteLine(String.Format("{0} {1} (Source: {2})", source.Name, source.TypeDescription, source.IsSource));
                if (source.ComponentType == MixerLineComponentType.SourceMicrophone)
                {
                    Debug.WriteLine(String.Format("Found the microphone: {0}", source.Name));
                    foreach (MixerControl control in source.Controls)
                    {
                        if (control.ControlType == MixerControlType.Volume)
                        {
                            Debug.WriteLine("Volume Found");
                            UnsignedMixerControl smc = (UnsignedMixerControl)control;
                            Debug.WriteLine(String.Format("Value: {0} ({1}-{2})", smc.Value, smc.MinValue, smc.MaxValue));

                        }
                    }
                }
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
            int sources = destination.SourceCount;            
            for (int sourceIndex = 0; sourceIndex < sources; sourceIndex++)
            {
                ExploreMixerSource(destination, sourceIndex);
            }
            int controls = destination.ControlsCount;
            for (int controlIndex = 0; controlIndex < controls; controlIndex++)
            {
                try
                {
                    var control = destination.GetControl(controlIndex);
                    Debug.WriteLine(String.Format("CONTROL: {0} {1}", control.Name, control.ControlType));
                }
                catch (MmException me)
                {
                    Debug.WriteLine(String.Format("MmException: {0}", me.Message));
                }
            }
        }

        private static void ExploreMixerSource(MixerLine destinationLine, int sourceIndex)
        {
            var sourceLine = destinationLine.GetSource(sourceIndex);
            int controls = sourceLine.ControlsCount;
            Debug.WriteLine(String.Format("Source {0}: {1}",
                sourceIndex, sourceLine));

            for (int controlIndex = 0; controlIndex < controls; controlIndex++)
            {
                var control = sourceLine.GetControl(controlIndex);
                Debug.WriteLine(String.Format("CONTROL: {0} {1}", control.Name, control.ControlType));
            }
        }
    }
}
