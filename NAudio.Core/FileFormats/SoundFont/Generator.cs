namespace NAudio.SoundFont
{
    /// <summary>
    /// Soundfont generator
    /// </summary>
    public class Generator
    {
        /// <summary>
        /// Gets the generator type
        /// </summary>
        public GeneratorEnum GeneratorType { get; set; }

        /// <summary>
        /// Generator amount as an unsigned short
        /// </summary>
        public ushort UInt16Amount { get; set; }

        /// <summary>
        /// Generator amount as a signed short
        /// </summary>
        public short Int16Amount
        {
            get => (short)UInt16Amount;
            set => UInt16Amount = (ushort)value;
        }

        /// <summary>
        /// Low byte amount
        /// </summary>
        public byte LowByteAmount
        {
            get
            {
                return (byte)(UInt16Amount & 0x00FF);
            }
            set
            {
                UInt16Amount &= 0xFF00;
                UInt16Amount += value;
            }
        }

        /// <summary>
        /// High byte amount
        /// </summary>
        public byte HighByteAmount
        {
            get
            {
                return (byte)((UInt16Amount & 0xFF00) >> 8);
            }
            set
            {
                UInt16Amount &= 0x00FF;
                UInt16Amount += (ushort)(value << 8);
            }
        }

        /// <summary>
        /// Instrument
        /// </summary>
        public Instrument Instrument { get; set; }

        /// <summary>
        /// Sample Header
        /// </summary>
        public SampleHeader SampleHeader { get; set; }

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        public override string ToString()
        {
            if (GeneratorType == GeneratorEnum.Instrument)
                return $"Generator Instrument {Instrument.Name}";
            else if (GeneratorType == GeneratorEnum.SampleID)
                return $"Generator SampleID {SampleHeader}";
            else
                return $"Generator {GeneratorType} {UInt16Amount}";
        }
    }
}