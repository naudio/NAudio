using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NAudio.Vst3;

/// <summary>
/// Reads and writes Steinberg <c>.vstpreset</c> files — the standard on-disk container for a
/// VST 3 plug-in's saved state. A preset bundles the component (DSP) state blob and, optionally,
/// the controller (UI / parameter) state blob, tagged with the plug-in's class identifier so a
/// host can refuse to load a preset that belongs to a different plug-in.
/// </summary>
/// <remarks>
/// <para>
/// This type handles only the binary container. To save or load the state of a <i>live</i> plug-in
/// use <see cref="Vst3Plugin.SavePreset(string)"/> / <see cref="Vst3Plugin.LoadPreset(string)"/>,
/// which capture and apply the component / controller blobs for you.
/// </para>
/// <para>
/// File layout (all integers little-endian, offsets relative to the start of the preset):
/// </para>
/// <code>
/// Header (48 bytes):  "VST3" | int32 version | char[32] classID | int64 chunkListOffset
/// Chunk data:         &lt;component bytes&gt;&lt;controller bytes&gt;&lt;meta-info bytes&gt;
/// Chunk list:         "List" | int32 entryCount | { char[4] id, int64 offset, int64 size } * n
/// </code>
/// <para>
/// The class ID stored in the header is the COM / GUID string form Steinberg uses on Windows
/// (<c>COM_COMPATIBLE</c>): the 16-byte <c>TUID</c> rendered as a 32-character hex string with the
/// first three GUID fields byte-swapped. The <see cref="Vst3PresetContents.ClassId"/> returned by
/// <see cref="Read(Stream)"/> is converted back to the raw-TUID hex used elsewhere in the library
/// (e.g. <see cref="Vst3ClassInfo.ClassId"/>), so the two are directly comparable.
/// </para>
/// </remarks>
public static class Vst3Preset
{
    private static ReadOnlySpan<byte> HeaderMagic => "VST3"u8;
    private static ReadOnlySpan<byte> ListMagic => "List"u8;

    /// <summary>Chunk id for the component (processor / DSP) state blob.</summary>
    public const string ComponentChunkId = "Comp";

    /// <summary>Chunk id for the controller (edit-controller / parameter) state blob.</summary>
    public const string ControllerChunkId = "Cont";

    /// <summary>Chunk id for the optional XML meta-info chunk.</summary>
    public const string MetaInfoChunkId = "Info";

    private const int FormatVersion = 1;
    private const int ClassIdSize = 32;

    /// <summary>
    /// Writes a <c>.vstpreset</c> to <paramref name="stream"/> (which must be seekable). The
    /// component blob is always written; the controller blob is written only when
    /// <paramref name="controllerState"/> is non-empty, and the meta-info chunk only when
    /// <paramref name="metaInfoXml"/> is non-null.
    /// </summary>
    /// <param name="stream">Destination stream; written from its current position and must be seekable.</param>
    /// <param name="classId">
    /// The plug-in's class id as 32 hex characters in raw-TUID order (i.e. the value of
    /// <see cref="Vst3ClassInfo.ClassId"/> / <see cref="Vst3Plugin.ClassInfo"/>). It is converted to
    /// the GUID string form on the wire.
    /// </param>
    /// <param name="componentState">The component (DSP) state blob.</param>
    /// <param name="controllerState">The controller state blob, or empty to omit the chunk.</param>
    /// <param name="metaInfoXml">Optional meta-info XML, or null to omit the chunk.</param>
    public static void Write(Stream stream, string classId, ReadOnlySpan<byte> componentState,
        ReadOnlySpan<byte> controllerState, string? metaInfoXml = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(classId);
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable to write a .vstpreset.", nameof(stream));
        }

        var classString = TuidHexToPresetClassId(classId);
        var startPos = stream.Position;
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        // Header — the chunk-list offset is patched in once the chunks have been written.
        writer.Write(HeaderMagic);
        writer.Write(FormatVersion);
        writer.Write(Encoding.ASCII.GetBytes(classString)); // exactly 32 bytes, no terminator
        var listOffsetFieldPos = stream.Position;
        writer.Write(0L); // placeholder

        var entries = new List<(string Id, long Offset, long Size)>(3);

        void WriteChunk(string id, ReadOnlySpan<byte> data)
        {
            var offset = stream.Position - startPos;
            writer.Write(data);
            entries.Add((id, offset, data.Length));
        }

        WriteChunk(ComponentChunkId, componentState);
        if (!controllerState.IsEmpty)
        {
            WriteChunk(ControllerChunkId, controllerState);
        }
        if (!string.IsNullOrEmpty(metaInfoXml))
        {
            WriteChunk(MetaInfoChunkId, Encoding.UTF8.GetBytes(metaInfoXml));
        }

        var listOffset = stream.Position - startPos;
        writer.Write(ListMagic);
        writer.Write(entries.Count);
        foreach (var (id, offset, size) in entries)
        {
            writer.Write(Encoding.ASCII.GetBytes(id)); // 4 bytes
            writer.Write(offset);
            writer.Write(size);
        }

        writer.Flush();
        var endPos = stream.Position;
        stream.Position = listOffsetFieldPos;
        writer.Write(listOffset);
        writer.Flush();
        stream.Position = endPos;
    }

    /// <summary>Reads and parses a <c>.vstpreset</c> from the given file.</summary>
    public static Vst3PresetContents Read(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        using var fs = File.OpenRead(filePath);
        return Read(fs);
    }

    /// <summary>
    /// Reads and parses a <c>.vstpreset</c> from <paramref name="stream"/> (which must be seekable).
    /// </summary>
    /// <exception cref="InvalidDataException">The stream is not a well-formed <c>.vstpreset</c>.</exception>
    public static Vst3PresetContents Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable to read a .vstpreset.", nameof(stream));
        }
        try
        {
            return ReadCore(stream);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("Truncated .vstpreset.", ex);
        }
    }

    private static Vst3PresetContents ReadCore(Stream stream)
    {
        var startPos = stream.Position;
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        var classString = ReadHeaderClassString(reader, out var version, out var listOffset);

        stream.Position = startPos + listOffset;
        Span<byte> listMagic = stackalloc byte[4];
        if (reader.Read(listMagic) != 4 || !listMagic.SequenceEqual(ListMagic))
        {
            throw new InvalidDataException("Malformed .vstpreset (missing 'List' chunk list).");
        }
        var entryCount = reader.ReadInt32();
        if (entryCount < 0 || entryCount > 1024)
        {
            throw new InvalidDataException($"Implausible .vstpreset chunk count ({entryCount}).");
        }

        byte[]? component = null;
        byte[]? controller = null;
        string? metaInfo = null;
        for (var i = 0; i < entryCount; i++)
        {
            var id = Encoding.ASCII.GetString(reader.ReadBytes(4));
            var offset = reader.ReadInt64();
            var size = reader.ReadInt64();
            // offset/size come straight off disk and may be hostile. Reject negatives and any chunk
            // that would run past the stream, using subtraction (not startPos + offset + size, which
            // can overflow Int64 and wrap past the check) so the bounds maths can't itself overflow.
            var available = stream.Length - startPos;
            if (offset < 0 || size < 0 || size > int.MaxValue
                || offset > available || size > available - offset)
            {
                throw new InvalidDataException($"Truncated .vstpreset (chunk '{id}' out of range).");
            }

            var resume = stream.Position;
            stream.Position = startPos + offset;
            var data = reader.ReadBytes((int)size);
            stream.Position = resume;

            switch (id)
            {
                case ComponentChunkId: component = data; break;
                case ControllerChunkId: controller = data; break;
                case MetaInfoChunkId: metaInfo = Encoding.UTF8.GetString(data); break;
                    // Other chunk ids (e.g. "Prog" program data) are ignored.
            }
        }

        if (component is null)
        {
            throw new InvalidDataException("Preset has no component ('Comp') chunk.");
        }

        return new Vst3PresetContents(
            PresetClassIdToTuidHex(classString), component, controller, metaInfo, version);
    }

    /// <summary>Reads just the class id from a <c>.vstpreset</c> file, without loading the state blobs.</summary>
    public static string ReadClassId(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        using var fs = File.OpenRead(filePath);
        return ReadClassId(fs);
    }

    /// <summary>
    /// Reads just the class id from a <c>.vstpreset</c> stream, without loading the state blobs.
    /// Returned in raw-TUID hex so it can be compared directly with <see cref="Vst3ClassInfo.ClassId"/>.
    /// </summary>
    public static string ReadClassId(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
            var classString = ReadHeaderClassString(reader, out _, out _);
            return PresetClassIdToTuidHex(classString);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("Truncated .vstpreset.", ex);
        }
    }

    private static string ReadHeaderClassString(BinaryReader reader, out int version, out long listOffset)
    {
        Span<byte> magic = stackalloc byte[4];
        if (reader.Read(magic) != 4 || !magic.SequenceEqual(HeaderMagic))
        {
            throw new InvalidDataException("Not a .vstpreset file (missing 'VST3' header).");
        }
        version = reader.ReadInt32();
        var classBytes = reader.ReadBytes(ClassIdSize);
        if (classBytes.Length < ClassIdSize)
        {
            throw new InvalidDataException("Truncated .vstpreset header.");
        }
        listOffset = reader.ReadInt64();
        return Encoding.ASCII.GetString(classBytes);
    }

    // Raw-TUID hex (32 chars, as the factory reports the cid) -> the GUID string form stored in the
    // file header. On Windows the SDK is COM_COMPATIBLE, so this is the standard GUID mixed-endian
    // rendering — exactly what System.Guid produces from / consumes as the 16 raw bytes.
    internal static string TuidHexToPresetClassId(string tuidHex)
    {
        if (tuidHex is null || tuidHex.Length != ClassIdSize)
        {
            throw new ArgumentException($"Class id must be {ClassIdSize} hex characters.", nameof(tuidHex));
        }
        Span<byte> bytes = stackalloc byte[16];
        if (Convert.FromHexString(tuidHex, bytes, out _, out var written) != OperationStatus.Done
            || written != 16)
        {
            throw new ArgumentException("Class id is not valid hexadecimal.", nameof(tuidHex));
        }
        return new Guid(bytes).ToString("N").ToUpperInvariant();
    }

    private static string PresetClassIdToTuidHex(string presetClassId)
    {
        var trimmed = presetClassId.Trim().TrimEnd('\0');
        if (!Guid.TryParseExact(trimmed, "N", out var guid))
        {
            throw new InvalidDataException(
                $"Preset class id '{presetClassId}' is not a valid VST 3 class identifier.");
        }
        return Convert.ToHexString(guid.ToByteArray());
    }
}

/// <summary>
/// The parsed contents of a <c>.vstpreset</c> file: the plug-in class id plus the state blobs.
/// Returned by <see cref="Vst3Preset.Read(Stream)"/>.
/// </summary>
public sealed class Vst3PresetContents
{
    internal Vst3PresetContents(string classId, byte[] componentState, byte[]? controllerState,
        string? metaInfoXml, int formatVersion)
    {
        ClassId = classId;
        ComponentState = componentState;
        ControllerState = controllerState;
        MetaInfoXml = metaInfoXml;
        FormatVersion = formatVersion;
    }

    /// <summary>
    /// The plug-in class id the preset belongs to, in raw-TUID hex (directly comparable with
    /// <see cref="Vst3ClassInfo.ClassId"/>).
    /// </summary>
    public string ClassId { get; }

    /// <summary>The component (processor / DSP) state blob. Always present.</summary>
    public byte[] ComponentState { get; }

    /// <summary>The controller state blob, or null when the preset has no controller chunk.</summary>
    public byte[]? ControllerState { get; }

    /// <summary>The meta-info XML, or null when the preset has no meta-info chunk.</summary>
    public string? MetaInfoXml { get; }

    /// <summary>The preset format version declared in the header.</summary>
    public int FormatVersion { get; }
}
