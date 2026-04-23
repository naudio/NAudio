using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Parsed contents of a <c>LIST</c> chunk with type <c>INFO</c> — the standard RIFF
    /// metadata container (artist, title, copyright, etc.).
    /// Use the named properties for common tags, or <see cref="this[string]"/> / <see cref="Contains"/>
    /// to access arbitrary four-character INFO subchunk ids.
    /// Also writable: construct, <see cref="Set"/> entries, then serialise via
    /// <see cref="WaveFileWriterExtensions.WriteInfoMetadata"/>.
    /// </summary>
    public sealed class InfoMetadata : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> entries;

        /// <summary>
        /// Creates an empty metadata container.
        /// </summary>
        public InfoMetadata() : this(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)) { }

        internal InfoMetadata(Dictionary<string, string> entries)
        {
            this.entries = entries;
        }

        /// <summary>
        /// Sets the value for the given four-character INFO subchunk id.
        /// Passing <c>null</c> or empty clears the entry. Id comparison is case-insensitive
        /// but stored uppercase.
        /// </summary>
        public void Set(string id, string value)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (id.Length != 4) throw new ArgumentException("INFO subchunk ids must be exactly four characters", nameof(id));
            var key = id.ToUpperInvariant();
            if (string.IsNullOrEmpty(value))
            {
                entries.Remove(key);
            }
            else
            {
                entries[key] = value;
            }
        }

        /// <summary>
        /// Number of INFO entries.
        /// </summary>
        public int Count => entries.Count;

        /// <summary>
        /// Gets the value for the given four-character INFO subchunk id (e.g. <c>INAM</c>),
        /// or <c>null</c> if not present. Case-insensitive.
        /// </summary>
        public string this[string id] => id != null && entries.TryGetValue(id.ToUpperInvariant(), out var value) ? value : null;

        /// <summary>
        /// Returns true if the given four-character INFO subchunk id is present. Case-insensitive.
        /// </summary>
        public bool Contains(string id) => id != null && entries.ContainsKey(id.ToUpperInvariant());

        /// <summary>Title / Name (<c>INAM</c>).</summary>
        public string Title => this["INAM"];

        /// <summary>Artist (<c>IART</c>).</summary>
        public string Artist => this["IART"];

        /// <summary>Album / Product (<c>IPRD</c>).</summary>
        public string Product => this["IPRD"];

        /// <summary>Comments (<c>ICMT</c>).</summary>
        public string Comments => this["ICMT"];

        /// <summary>Copyright (<c>ICOP</c>).</summary>
        public string Copyright => this["ICOP"];

        /// <summary>Creation date (<c>ICRD</c>).</summary>
        public string CreationDate => this["ICRD"];

        /// <summary>Engineer (<c>IENG</c>).</summary>
        public string Engineer => this["IENG"];

        /// <summary>Genre (<c>IGNR</c>).</summary>
        public string Genre => this["IGNR"];

        /// <summary>Keywords (<c>IKEY</c>).</summary>
        public string Keywords => this["IKEY"];

        /// <summary>Software that created this file (<c>ISFT</c>).</summary>
        public string Software => this["ISFT"];

        /// <summary>Source (<c>ISRC</c>).</summary>
        public string Source => this["ISRC"];

        /// <summary>Technician (<c>ITCH</c>).</summary>
        public string Technician => this["ITCH"];

        /// <summary>Subject (<c>ISBJ</c>).</summary>
        public string Subject => this["ISBJ"];

        /// <summary>Track number (<c>ITRK</c>).</summary>
        public string TrackNumber => this["ITRK"];

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Serialises this metadata into the body of a <c>LIST</c> RIFF chunk of type <c>INFO</c>
        /// (no chunk id / size header — starts with the <c>INFO</c> type marker).
        /// Values are written as null-terminated ASCII strings.
        /// </summary>
        internal byte[] ToInfoListChunkData()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(Encoding.ASCII.GetBytes("INFO"));
            foreach (var kv in entries)
            {
                var idBytes = Encoding.ASCII.GetBytes(kv.Key);
                if (idBytes.Length != 4) continue; // defensive; Set() already validates
                var valueBytes = Encoding.UTF8.GetBytes(kv.Value ?? string.Empty);
                int size = valueBytes.Length + 1; // null terminator
                w.Write(idBytes);
                w.Write(size);
                w.Write(valueBytes);
                w.Write((byte)0);
                if ((size & 1) == 1) w.Write((byte)0); // word-align
            }
            return ms.ToArray();
        }
    }

    /// <summary>
    /// <see cref="IWaveChunkInterpreter{T}"/> that parses the <c>LIST/INFO</c> chunk.
    /// Returns <c>null</c> if no INFO list is present. If multiple LIST chunks are present
    /// (e.g. an <c>adtl</c> list for cue labels alongside an <c>INFO</c> list), the INFO list
    /// is located by its type header.
    /// </summary>
    public sealed class InfoListInterpreter : IWaveChunkInterpreter<InfoMetadata>
    {
        /// <summary>
        /// Shared stateless instance.
        /// </summary>
        public static readonly InfoListInterpreter Instance = new InfoListInterpreter();

        /// <inheritdoc />
        public InfoMetadata Interpret(WaveChunks chunks)
        {
            if (chunks == null) return null;

            foreach (var list in chunks.FindAll("LIST"))
            {
                if (list.Length < 4) continue;
                var data = chunks.GetData(list);
                if (data.Length < 4) continue;
                if (data[0] != (byte)'I' || data[1] != (byte)'N' || data[2] != (byte)'F' || data[3] != (byte)'O') continue;

                var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int p = 4;
                while (data.Length - p >= 8)
                {
                    string id = Encoding.ASCII.GetString(data, p, 4);
                    int size = BitConverter.ToInt32(data, p + 4);
                    if (size < 0 || data.Length - p - 8 < size) break;

                    // Null-terminated string. The RIFF spec calls for "ZSTR" but does not
                    // mandate an encoding; NAudio writes UTF-8 so that arbitrary Unicode
                    // characters round-trip, and reads the same way.
                    int end = p + 8;
                    int stringEnd = end;
                    int limit = end + size;
                    while (stringEnd < limit && data[stringEnd] != 0) stringEnd++;
                    entries[id] = Encoding.UTF8.GetString(data, end, stringEnd - end);

                    int advance = 8 + size;
                    if ((size & 1) == 1) advance++; // word alignment padding
                    p += advance;
                }

                return new InfoMetadata(entries);
            }

            return null;
        }
    }
}
