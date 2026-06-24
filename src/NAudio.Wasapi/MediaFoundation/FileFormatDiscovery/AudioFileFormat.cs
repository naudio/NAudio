using System;
using System.IO;
using System.Runtime.CompilerServices;

#pragma warning disable CS0659 // AudioFileFormat is not overriding GetHashCode

namespace NAudio.MediaFoundation.FileFormatDiscovery;

/// <summary>
/// Defines a way for determining whether a data stream is of a specified audio file format. <br />
/// Derivants of this class define the finder implementation and must be singletons.
/// </summary>
internal abstract class AudioFileFormat : IEquatable<AudioFileFormat>
{
    /// <summary>
    /// Gets the MIME type name for this audio file format.
    /// </summary>
    public abstract string MimeTypeName { get; }

    /// <summary>
    /// Gets a value whether the specified stream is the audio file format represented by this instance.
    /// </summary>
    /// <param name="stream">The data stream.</param>
    /// <returns>A <see cref="bool"/> value determining whether specified file is the audio file format represented by the instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public abstract bool IsFormat(Stream stream);

    /// <summary>
    /// Gets the MIME type of this audio file format.
    /// </summary>
    public string MimeType => $"audio/{MimeTypeName}";

    /// <summary>
    /// Gets a value whether the current audio file format is the same audio file format.
    /// </summary>
    /// <param name="other">The other file format to compare this one against.</param>
    /// <returns>A value whether this object and <paramref name="other"/> are the same audio file formats.</returns>
    public bool Equals(AudioFileFormat other) => ReferenceEquals(this, other);

    /// <inheritdoc />
    public sealed override bool Equals(object obj) => ReferenceEquals(this, obj);
}
