using System;
using System.Runtime.InteropServices;

namespace NAudio.Midi
{
    /// <summary>
    /// class representing the capabilities of a MIDI out device
    /// MIDIOUTCAPS: http://msdn.microsoft.com/en-us/library/dd798467%28VS.85%29.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MidiOutCapabilities
    {
        Int16 manufacturerId;
        Int16 productId;
        int driverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        string productName;
        Int16 wTechnology;
        Int16 wVoices;
        Int16 wNotes;
        UInt16 wChannelMask;
        MidiOutCapabilityFlags dwSupport;

        const int MaxProductNameLength = 32; // max product name length (including NULL)

        [Flags]
        enum MidiOutCapabilityFlags
        {
            /// <summary>
            /// MIDICAPS_VOLUME
            /// </summary>
            Volume = 1,
            /// <summary>
            /// separate left-right volume control
            /// MIDICAPS_LRVOLUME
            /// </summary>
            LeftRightVolume = 2,
            /// <summary>
            /// MIDICAPS_CACHE
            /// </summary>
            PatchCaching = 4,
            /// <summary>
            /// MIDICAPS_STREAM
            /// driver supports midiStreamOut directly
            /// </summary>
            Stream = 8,
        }

        /// <summary>
        /// Gets the manufacturer of this device
        /// </summary>
        public Manufacturers Manufacturer
        {
            get
            {
                return (Manufacturers)manufacturerId;
            }
        }

        /// <summary>
        /// Gets the product identifier (manufacturer specific)
        /// </summary>
        public short ProductId
        {
            get
            {
                return productId;
            }
        }

        /// <summary>
        /// Gets the product name
        /// </summary>
        public String ProductName
        {
            get
            {
                return productName;
            }
        }

        /// <summary>
        /// Returns the number of supported voices
        /// </summary>
        public int Voices
        {
            get
            {
                return wVoices;
            }
        }

        /// <summary>
        /// Gets the polyphony of the device
        /// </summary>
        public int Notes
        {
            get
            {
                return wNotes;
            }
        }

        /// <summary>
        /// Returns true if the device supports all channels
        /// </summary>
        public bool SupportsAllChannels
        {
            get
            {
                return wChannelMask == 0xFFFF;
            }
        }

        /// <summary>
        /// Queries whether a particular channel is supported
        /// </summary>
        /// <param name="channel">Channel number to test</param>
        /// <returns>True if the channel is supported</returns>
        public bool SupportsChannel(int channel)
        {
            return (wChannelMask & (1 << (channel - 1))) > 0;
        }

        /// <summary>
        /// Returns true if the device supports patch caching
        /// </summary>
        public bool SupportsPatchCaching
        {
            get
            {
                return (dwSupport & MidiOutCapabilityFlags.PatchCaching) != 0;
            }
        }

        /// <summary>
        /// Returns true if the device supports separate left and right volume
        /// </summary>
        public bool SupportsSeparateLeftAndRightVolume
        {
            get
            {
                return (dwSupport & MidiOutCapabilityFlags.LeftRightVolume) != 0;
            }
        }

        /// <summary>
        /// Returns true if the device supports MIDI stream out
        /// </summary>
        public bool SupportsMidiStreamOut
        {
            get
            {
                return (dwSupport & MidiOutCapabilityFlags.Stream) != 0;
            }
        }

        /// <summary>
        /// Returns true if the device supports volume control
        /// </summary>
        public bool SupportsVolumeControl
        {
            get
            {
                return (dwSupport & MidiOutCapabilityFlags.Volume) != 0;
            }
        }

        /// <summary>
        /// Returns the type of technology used by this MIDI out device
        /// </summary>
        public MidiOutTechnology Technology
        {
            get
            {
                return (MidiOutTechnology)wTechnology;
            }
        }

    }
}
