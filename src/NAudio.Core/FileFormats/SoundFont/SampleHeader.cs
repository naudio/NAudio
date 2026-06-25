namespace NAudio.SoundFont;

/// <summary>
/// A SoundFont Sample Header
/// </summary>
public class SampleHeader
{
    /// <summary>
    /// The sample name
    /// </summary>
    public string SampleName { get; set; }
    /// <summary>
    /// Start offset
    /// </summary>
    public uint Start { get; set; }
    /// <summary>
    /// End offset
    /// </summary>
    public uint End { get; set; }
    /// <summary>
    /// Start loop point
    /// </summary>
    public uint StartLoop { get; set; }
    /// <summary>
    /// End loop point
    /// </summary>
    public uint EndLoop { get; set; }
    /// <summary>
    /// Sample Rate
    /// </summary>
    public uint SampleRate { get; set; }
    /// <summary>
    /// Original pitch
    /// </summary>
    public byte OriginalPitch { get; set; }
    /// <summary>
    /// Pitch correction
    /// </summary>
    public sbyte PitchCorrection { get; set; }
    /// <summary>
    /// Sample Link
    /// </summary>
    public ushort SampleLink { get; set; }
    /// <summary>
    /// SoundFont Sample Link Type
    /// </summary>
    public SFSampleLink SFSampleLink { get; set; }

    /// <summary>
    /// <see cref="object.ToString"/>
    /// </summary>
    public override string ToString() => SampleName;
}
