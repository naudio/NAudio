using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// Holds information on a cue: a labeled position within a Wave file
/// </summary>
public class Cue
{
    /// <summary>
    /// Cue position in samples
    /// </summary>
    public int Position { get; }
    /// <summary>
    /// Label of the cue
    /// </summary>
    public string Label { get; }
    /// <summary>
    /// Cue length in samples, or <c>null</c> if the cue is a point marker rather than
    /// a region. Populated from the <c>ltxt</c> sub-chunk of a WAV <c>LIST/adtl</c> chunk.
    /// </summary>
    public int? Length { get; }

    /// <summary>
    /// Creates a Cue based on a sample position and label
    /// </summary>
    /// <param name="position"></param>
    /// <param name="label"></param>
    public Cue(int position, string label) : this(position, label, null)
    {
    }

    /// <summary>
    /// Creates a Cue based on a sample position, label and region length in samples.
    /// </summary>
    /// <param name="position">Sample position of the cue point.</param>
    /// <param name="label">Text label, or <c>null</c> for an empty label.</param>
    /// <param name="length">Length in samples of the region starting at <paramref name="position"/>,
    /// or <c>null</c> for a point marker with no length.</param>
    public Cue(int position, string label, int? length)
    {
        Position = position;
        Label = label ?? string.Empty;
        Length = length;
    }
}

/// <summary>
/// Holds a list of cues
/// </summary>
/// <remarks>
/// The specs for reading and writing cues from the cue and list RIFF chunks 
/// are from http://www.sonicspot.com/guide/wavefiles.html and http://www.wotsit.org/
/// ------------------------------
/// The cues are stored like this:
/// ------------------------------
/// struct CuePoint
/// {
///  Int32 dwIdentifier;
///  Int32 dwPosition;
///  Int32 fccChunk;
///  Int32 dwChunkStart;
///  Int32 dwBlockStart;
///  Int32 dwSampleOffset;
/// } 
///
/// struct CueChunk
/// {
///  Int32 chunkID;
///  Int32 chunkSize;
///  Int32 dwCuePoints;
///  CuePoint[] points;
/// }
/// ------------------------------
/// Labels look like this:
/// ------------------------------
/// struct ListHeader 
/// {
///   Int32 listID;      /* 'list' */
///   Int32 chunkSize;   /* includes the Type ID below */
///   Int32 typeID;      /* 'adtl' */
/// } 
///
/// struct LabelChunk
/// {
///   Int32 chunkID;
///   Int32 chunkSize;
///   Int32 dwIdentifier;
///   Char[] dwText;  /* null-terminated; RIFF does not mandate an encoding — NAudio uses UTF-8 */
/// } LabelChunk;
///
/// struct TextWithDataLengthChunk
/// {
///   Int32 chunkID;         /* 'ltxt' */
///   Int32 chunkSize;
///   Int32 dwIdentifier;
///   Int32 dwSampleLength;
///   Int32 dwPurpose;
///   Int16 wCountry;
///   Int16 wLanguage;
///   Int16 wDialect;
///   Int16 wCodePage;
///   /* optional trailing text may follow */
/// } TextWithDataLengthChunk;
/// </remarks>
public class CueList
{
    private readonly List<Cue> cues = new();
    /// <summary>
    /// Creates an empty cue list
    /// </summary>
    public CueList()
    {

    }

    /// <summary>
    /// Adds an item to the list
    /// </summary>
    /// <param name="cue">Cue</param>
    public void Add(Cue cue)
    {
        cues.Add(cue);
    }

    /// <summary>
    /// Gets sample positions for the embedded cues
    /// </summary>
    /// <returns>Array containing the cue positions</returns>
    public int[] CuePositions
    {
        get
        {
            int[] positions = new int[cues.Count];
            for (int i = 0; i < cues.Count; i++)
            {
                positions[i] = cues[i].Position;
            }
            return positions;
        }
    }

    /// <summary>
    /// Gets labels for the embedded cues
    /// </summary>
    /// <returns>Array containing the labels</returns>
    public string[] CueLabels
    {
        get
        {
            string[] labels = new string[cues.Count];
            for (int i = 0; i < cues.Count; i++)
            {
                labels[i] = cues[i].Label;
            }
            return labels;
        }
    }

    /// <summary>
    /// Gets region lengths in samples for the embedded cues. Entries are <c>null</c>
    /// for cues that are point markers rather than regions.
    /// </summary>
    /// <returns>Array containing the cue lengths.</returns>
    public int?[] CueLengths
    {
        get
        {
            int?[] lengths = new int?[cues.Count];
            for (int i = 0; i < cues.Count; i++)
            {
                lengths[i] = cues[i].Length;
            }
            return lengths;
        }
    }

    /// <summary>
    /// Creates a cue list from the cue RIFF chunk and the list RIFF chunk
    /// </summary>
    /// <param name="cueChunkData">The data contained in the cue chunk</param>
    /// <param name="listChunkData">The data contained in the list chunk</param>
    internal CueList(byte[] cueChunkData, byte[] listChunkData)
    {
        int cueCount = BitConverter.ToInt32(cueChunkData, 0);
        Dictionary<int, int> cueIndex = new Dictionary<int, int>();
        int[] positions = new int[cueCount];
        int cue = 0;

        for (int p = 4; cueChunkData.Length - p >= 24; p += 24, cue++)
        {
            cueIndex[BitConverter.ToInt32(cueChunkData, p)] = cue;
            positions[cue] = BitConverter.ToInt32(cueChunkData, p + 20);
        }

        string[] labels = new string[cueCount];
        int?[] lengths = new int?[cueCount];
        var labelChunkId = ChunkIdentifier.ChunkIdentifierToInt32("labl");
        var ltxtChunkId = ChunkIdentifier.ChunkIdentifierToInt32("ltxt");

        // Parse list chunk - properly handle all chunk types.
        // listChunkData may be null when the file has cue points but no adtl labels
        // (e.g. unnamed markers written by Wavosaur); in that case the cues keep empty labels.
        for (int p = 4; listChunkData != null && listChunkData.Length - p >= 8;)
        {
            int chunkId = BitConverter.ToInt32(listChunkData, p);
            int chunkSize = BitConverter.ToInt32(listChunkData, p + 4);

            if (chunkId == labelChunkId && chunkSize >= 4 && listChunkData.Length - p >= chunkSize + 8)
            {
                // This is a label chunk - extract the label data
                int labelLength = chunkSize - 4; // chunkSize includes the dwIdentifier
                if (labelLength > 0 && listChunkData.Length - p - 12 >= labelLength - 1)
                {
                    var cueId = BitConverter.ToInt32(listChunkData, p + 8);

                    // Validate that the cue ID exists before accessing the dictionary
                    if (cueIndex.TryGetValue(cueId, out var cueIndex_value))
                    {
                        labels[cueIndex_value] = Encoding.UTF8.GetString(listChunkData, p + 12, labelLength - 1);
                    }
                }
            }
            else if (chunkId == ltxtChunkId && chunkSize >= 20 && listChunkData.Length - p >= chunkSize + 8)
            {
                // Text-with-data-length: carries a region length (dwSampleLength) for a cue point.
                // Minimum payload is 20 bytes: dwIdentifier + dwSampleLength + dwPurpose + 4x Int16.
                var cueId = BitConverter.ToInt32(listChunkData, p + 8);
                if (cueIndex.TryGetValue(cueId, out var cueIndex_value))
                {
                    lengths[cueIndex_value] = BitConverter.ToInt32(listChunkData, p + 12);
                }
            }

            // Move to next chunk: account for proper word-alignment padding
            // chunkSize is the size of the chunk data, add 8 for chunk ID and size fields
            int chunkTotalSize = chunkSize + 8;
            // Add padding if chunk data size is odd (word alignment)
            if (chunkSize % 2 == 1)
            {
                chunkTotalSize += 1;
            }
            p += chunkTotalSize;
        }

        for (int i = 0; i < cueCount; i++)
        {
            cues.Add(new Cue(positions[i], labels[i], lengths[i]));
        }
    }

    /// <summary>
    /// Serialises the cue points into the body of a <c>cue </c> RIFF chunk
    /// (no chunk id / size header).
    /// </summary>
    internal byte[] SerializeCueChunkData()
    {
        int dataChunkId = ChunkIdentifier.ChunkIdentifierToInt32("data");
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Count);
        for (int i = 0; i < Count; i++)
        {
            int position = this[i].Position;
            w.Write(i);               // dwIdentifier
            w.Write(position);        // dwPosition
            w.Write(dataChunkId);     // fccChunk
            w.Write(0);               // dwChunkStart
            w.Write(0);               // dwBlockStart
            w.Write(position);        // dwSampleOffset
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Serialises the cue labels (and optional region lengths) into the body of a <c>LIST</c>
    /// chunk of type <c>adtl</c> (no chunk id / size header — starts with the <c>adtl</c>
    /// type marker). A cue whose <see cref="Cue.Length"/> is non-null gets a companion
    /// <c>ltxt</c> sub-chunk emitted alongside its <c>labl</c>.
    /// </summary>
    internal byte[] SerializeAdtlListChunkData()
    {
        int labelChunkId = ChunkIdentifier.ChunkIdentifierToInt32("labl");
        int ltxtChunkId = ChunkIdentifier.ChunkIdentifierToInt32("ltxt");
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(Encoding.UTF8.GetBytes("adtl"));
        for (int i = 0; i < Count; i++)
        {
            var labelArray = Encoding.UTF8.GetBytes(this[i].Label ?? string.Empty);
            w.Write(labelChunkId);
            w.Write(labelArray.Length + 1 + 4); // dwIdentifier + text + null terminator
            w.Write(i);                         // dwIdentifier (the cue id)
            w.Write(labelArray);
            w.Write((byte)0);                   // null terminator
            if ((labelArray.Length + 1) % 2 == 1)
            {
                w.Write((byte)0);               // word-alignment padding
            }

            if (this[i].Length.HasValue)
            {
                // ltxt minimum payload: dwIdentifier + dwSampleLength + dwPurpose + 4x Int16 = 20 bytes.
                // dwPurpose and the four language fields are left at 0 — consumers that only care
                // about region length ignore them, and there's no widely-honoured semantic default.
                w.Write(ltxtChunkId);
                w.Write(20);
                w.Write(i);                     // dwIdentifier (cue id)
                w.Write(this[i].Length.Value);  // dwSampleLength
                w.Write(0);                     // dwPurpose
                w.Write((short)0);              // wCountry
                w.Write((short)0);              // wLanguage
                w.Write((short)0);              // wDialect
                w.Write((short)0);              // wCodePage
            }
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Number of cues
    /// </summary>
    public int Count => cues.Count;

    /// <summary>
    /// Accesses the cue at the specified index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Cue this[int index] => cues[index];
}

/// <summary>
/// <see cref="IWaveChunkInterpreter{T}"/> that reads the <c>cue</c> and companion <c>LIST/adtl</c>
/// chunks from a WAV file and returns a <see cref="CueList"/>.
/// Returns <c>null</c> only when no <c>cue </c> chunk is present. When a <c>cue </c> chunk
/// exists without a companion <c>LIST/adtl</c> label chunk, the cues are returned with empty labels.
/// </summary>
public sealed class CueListInterpreter : IWaveChunkInterpreter<CueList>
{
    /// <summary>
    /// Shared stateless instance.
    /// </summary>
    public static readonly CueListInterpreter Instance = new();

    /// <inheritdoc />
    public CueList Interpret(WaveChunks chunks)
    {
        if (chunks == null) return null;
        var cueChunk = chunks.Find("cue ");
        if (cueChunk == null) return null;

        // A WAV file may contain multiple LIST chunks (e.g. INFO metadata alongside adtl labels).
        // Only the adtl list carries cue labels, so we filter by list type.
        byte[] listChunkData = null;
        foreach (var list in chunks.FindAll("LIST"))
        {
            if (list.Length < 4) continue;
            var data = chunks.GetData(list);
            if (data.Length >= 4 && data[0] == (byte)'a' && data[1] == (byte)'d' && data[2] == (byte)'t' && data[3] == (byte)'l')
            {
                listChunkData = data;
                break;
            }
        }

        // listChunkData may be null here: the file has cue points but no adtl labels
        // (e.g. unnamed markers). The cues are still returned, with empty labels.
        var cueChunkData = chunks.GetData(cueChunk);
        return new CueList(cueChunkData, listChunkData);
    }
}
