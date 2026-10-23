
using NAudio.Wave;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides several options that control over how an
/// extended file writer instance should be initialized.
/// </summary>
public class ExtendedAudioFileWriterSettings
{
    /// <summary>
    /// Gets/sets the MIME type of the file to produce. <br />
    /// The writer will utilize this to determine
    /// the format of the audio file.
    /// </summary>
    public required string FileType { get; init; }

    /// <summary>
    /// Gets/sets the format under which data are provided
    /// to the writer. This differs from the actual format
    /// the data are written into, which may be a compressed
    /// format - selected by the writer once initialized.
    /// </summary>
    public required WaveFormat ProvidingFormat { get; init; }

    /// <summary>
    /// Set to <see langword="true"/> to instruct the 
    /// writer, if possible, to specify the output file
    /// as IEEE floating-point samples. Typically
    /// is only used for uncompressed formats,
    /// and it is not meaningful when using a compressed format.
    /// </summary>
    public bool EmitIEEEFloatSamples { get; init; } = false;

    /// <summary>
    /// For compressed formats, this defines the number of frames
    /// that each compressed packet will have. By default it is set to 1024.
    /// </summary>
    public int FramesPerPacket { get; init; } = 1024;
}
