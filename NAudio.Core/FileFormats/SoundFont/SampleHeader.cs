namespace NAudio.SoundFont
{
    /// <summary>
    /// A SoundFont Sample Header
    /// </summary>
    public class SampleHeader
    {
        /// <summary>
        /// The sample name
        /// </summary>
        public string SampleName;
        /// <summary>
        /// Start offset
        /// </summary>
        public uint Start;
        /// <summary>
        /// End offset
        /// </summary>
        public uint End;
        /// <summary>
        /// Start loop point
        /// </summary>
        public uint StartLoop;
        /// <summary>
        /// End loop point
        /// </summary>
        public uint EndLoop;
        /// <summary>
        /// Sample Rate
        /// </summary>
        public uint SampleRate;
        /// <summary>
        /// Original pitch
        /// </summary>
        public byte OriginalPitch;
        /// <summary>
        /// Pitch correction
        /// </summary>
        public sbyte PitchCorrection;
        /// <summary>
        /// Sample Link
        /// </summary>
        public ushort SampleLink;
        /// <summary>
        /// SoundFont Sample Link Type
        /// </summary>
        public SFSampleLink SFSampleLink;

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        public override string ToString() => SampleName;

    }
}

