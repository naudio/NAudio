using System.IO;
using System.Text;

namespace NAudio.Midi;

/// <summary>
/// Represents a MIDI meta event with raw data
/// </summary>
public class RawMetaEvent : MetaEvent
{
    /// <summary>
    /// Raw data contained in the meta event
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    ///  Creates a meta event with raw data
    /// </summary>
    /// <param name="metaEventType">The meta event type</param>
    /// <param name="absoluteTime">Absolute time of this event</param>
    /// <param name="data">The raw data. <see langword="null"/> is treated as empty.</param>
    public RawMetaEvent(MetaEventType metaEventType, long absoluteTime, byte[]? data) : base(metaEventType, data?.Length ?? 0, absoluteTime)
    {
        Data = data ?? [];
    }

    /// <summary>
    /// Creates a deep clone of this MIDI event.
    /// </summary>
    public override MidiEvent Clone() => new RawMetaEvent(MetaEventType, AbsoluteTime, (byte[])Data.Clone());

    /// <summary>
    /// Describes this meta event
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder().Append(base.ToString());
        foreach (var b in Data)
            sb.AppendFormat(" {0:X2}", b);
        return sb.ToString();
    }

    /// <summary>
    /// <see cref="MidiEvent.Export"/>
    /// </summary>
    public override void Export(ref long absoluteTime, BinaryWriter writer)
    {
        base.Export(ref absoluteTime, writer);
        writer.Write(Data, 0, Data.Length);
    }
}
