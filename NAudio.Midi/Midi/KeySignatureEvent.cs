using System;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI key signature event event
    /// </summary>
    public class KeySignatureEvent : MetaEvent
    {
        private readonly byte sharpsFlats;
        private readonly byte majorMinor;

        private static readonly string[] MajorKeyNames =
        {
            "Cb", "Gb", "Db", "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#"
        };

        private static readonly string[] MinorKeyNames =
        {
            "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#"
        };

        /// <summary>
        /// Reads a new track sequence number event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">the data length</param>
        public KeySignatureEvent(BinaryReader br, int length)
        {
            if (length != 2)
            {
                throw new FormatException("Invalid key signature length");
            }

            var sharpsFlatsByte = br.ReadByte(); // sf=sharps/flats (-7=7 flats, 0=key of C,7=7 sharps)
            var majorMinorByte = br.ReadByte(); // mi=major/minor (0=major, 1=minor)

            if ((sbyte)sharpsFlatsByte < -7 || (sbyte)sharpsFlatsByte > 7)
            {
                throw new FormatException($"Invalid key signature sharps/flats value {(sbyte)sharpsFlatsByte}. Expected range is -7 to 7.");
            }

            if (majorMinorByte > 1)
            {
                throw new FormatException($"Invalid key signature major/minor value {majorMinorByte}. Expected 0 (major) or 1 (minor).");
            }

            sharpsFlats = sharpsFlatsByte;
            majorMinor = majorMinorByte;
        }

        /// <summary>
        /// Creates a new Key signature event with the specified data
        /// </summary>
        public KeySignatureEvent(int sharpsFlats, int majorMinor, long absoluteTime)
            : base(MetaEventType.KeySignature, 2, absoluteTime)
        {
            if (sharpsFlats < -7 || sharpsFlats > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(sharpsFlats), sharpsFlats,
                    "Sharps/flats value must be in the range -7 to 7.");
            }

            if (majorMinor < 0 || majorMinor > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(majorMinor), majorMinor,
                    "Major/minor value must be 0 (major) or 1 (minor).");
            }

            this.sharpsFlats = (byte)sharpsFlats;
            this.majorMinor = (byte)majorMinor;
        }

        /// <summary>
        /// Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone() => (KeySignatureEvent)MemberwiseClone();

        /// <summary>
        /// Number of sharps or flats
        /// </summary>
        public int SharpsFlats => (sbyte)sharpsFlats;

        /// <summary>
        /// Major or Minor key
        /// </summary>
        public int MajorMinor => majorMinor;

        /// <summary>
        /// The musical key name represented by this event (for example Bb major, C# minor)
        /// </summary>
        public string KeyName
        {
            get
            {
                var index = SharpsFlats + 7;
                var tonic = MajorMinor == 0 ? MajorKeyNames[index] : MinorKeyNames[index];
                return MajorMinor == 0 ? $"{tonic} major" : $"{tonic} minor";
            }
        }

        /// <summary>
        /// Describes this event
        /// </summary>
        /// <returns>String describing the event</returns>
        public override string ToString()
        {
            return $"{base.ToString()} {KeyName}";
        }

        /// <summary>
        /// Calls base class export first, then exports the data 
        /// specific to this event
        /// <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(sharpsFlats);
            writer.Write(majorMinor);
        }
    }
}