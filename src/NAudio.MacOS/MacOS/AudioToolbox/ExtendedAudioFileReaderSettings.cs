
using NAudio.Wave;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides several options that control over how an
/// extended file reader instance should be initialized.
/// </summary>
public class ExtendedAudioFileReaderSettings
{
    /// <summary>
    /// When set to <see langword="true"/>, instructs the reader to provide 32-bit IEEE floating-point samples,
    /// instead of what the file provides.
    /// </summary>
    public bool RequestIeeeFloat { get; set; }

    /// <summary>
    /// When set to <see langword="true"/>, instructs the reader to 
    /// directly use the bit rate defined in the file if there is one. <br />
    /// If specified to <see langword="false" />, the reader will convert 
    /// the bit rate to the closest power of two. 
    /// If, for example, a file is encoded in 24-bit, the reader
    /// will enforce a 32-bit output data format. <br />
    /// If you do not want to manipulate the audio data produced by the reader, 
    /// you can safely set this to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Note that this property is exclusive to the <see cref="RequestIeeeFloat"/> as
    /// it will take precendence over this one and the reader will go 
    /// ahead and specify IEEE 32-bit depth.
    /// </remarks>
    public bool AllowNonPowerOfTwoBitRates { get; set; }

    /// <summary>
    /// A completely specified PCM or IEEE floating-point <see cref="WaveFormat"/>
    /// that can be used to specify the audio format under which the file will be 
    /// read as. Can also specify a <see cref="WaveFormatExtensible"/> instance here. <br />
    /// Defining <see langword="null"/> here instructs the reader to use it's built-in logic
    /// to define the output format.
    /// </summary>
    /// <remarks>
    /// Note that this property, when appropriately defined, is exclusive to both
    /// <see cref="RequestIeeeFloat"/> and <see cref="AllowNonPowerOfTwoBitRates"/>
    /// properties as they are purely existing to manipulate the default built-in logic
    /// of the reader.
    /// </remarks>
    public WaveFormat OutputFormat { get; set; }
}
