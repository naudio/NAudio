using System;
using System.Buffers.Binary;

namespace NAudio.Wave;

/// <summary>
/// Represents a Xing VBR header
/// </summary>
public class XingHeader
{
    [Flags]
    private enum XingHeaderOptions
    {
        Frames = 1,
        Bytes = 2,
        Toc = 4,
        VbrScale = 8
    }

    private int vbrScale = -1;
    private int startOffset;
    private int endOffset;

    private int tocOffset = -1;
    private int framesOffset = -1;
    private int bytesOffset = -1;
    private readonly Mp3Frame frame;
    private readonly byte[] rawData;

    private static int ReadBigEndian(byte[] buffer, int offset)
    {
        ReadOnlySpan<byte> bytes = buffer.AsSpan(offset, 4);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    private void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset, 4), value);
    }

    /// <summary>
    /// Load Xing Header
    /// </summary>
    /// <param name="frame">Frame</param>
    /// <returns>Xing Header</returns>
    public static XingHeader? LoadXingHeader(Mp3Frame frame)
    {
        if (frame.RawData is not { } rawData)
        {
            return null; // frame was loaded without its data
        }
        XingHeader xingHeader = new XingHeader(frame, rawData);
        int offset = 0;

        if (frame.MpegVersion == MpegVersion.Version1)
        {
            if (frame.ChannelMode != ChannelMode.Mono)
                offset = 32 + 4;
            else
                offset = 17 + 4;
        }
        else if (frame.MpegVersion == MpegVersion.Version2)
        {
            if (frame.ChannelMode != ChannelMode.Mono)
                offset = 17 + 4;
            else
                offset = 9 + 4;
        }
        else
        {
            return null;
            // throw new FormatException("Unsupported MPEG Version");
        }

        // A tiny first frame (e.g. MPEG-2 at 8 kbps is 24 bytes) can't hold a Xing header at
        // all; a frame that holds the tag but not the fields its flags announce is truncated
        // or corrupt. Either way there is no usable header, not an out-of-range read.
        if (rawData.Length < offset + 8)
        {
            return null;
        }

        if ((rawData[offset + 0] == 'X') &&
            (rawData[offset + 1] == 'i') &&
            (rawData[offset + 2] == 'n') &&
            (rawData[offset + 3] == 'g'))
        {
            xingHeader.startOffset = offset;
            offset += 4;
        }
        else if ((rawData[offset + 0] == 'I') &&
                 (rawData[offset + 1] == 'n') &&
                 (rawData[offset + 2] == 'f') &&
                 (rawData[offset + 3] == 'o'))
        {
            xingHeader.startOffset = offset;
            offset += 4;
        }
        else
        {
            return null;
        }

        XingHeaderOptions flags = (XingHeaderOptions)ReadBigEndian(rawData, offset);
        offset += 4;

        int fieldsLength = ((flags & XingHeaderOptions.Frames) != 0 ? 4 : 0)
            + ((flags & XingHeaderOptions.Bytes) != 0 ? 4 : 0)
            + ((flags & XingHeaderOptions.Toc) != 0 ? 100 : 0)
            + ((flags & XingHeaderOptions.VbrScale) != 0 ? 4 : 0);
        if (rawData.Length < offset + fieldsLength)
        {
            return null;
        }

        if ((flags & XingHeaderOptions.Frames) != 0)
        {
            xingHeader.framesOffset = offset;
            offset += 4;
        }
        if ((flags & XingHeaderOptions.Bytes) != 0)
        {
            xingHeader.bytesOffset = offset;
            offset += 4;
        }
        if ((flags & XingHeaderOptions.Toc) != 0)
        {
            xingHeader.tocOffset = offset;
            offset += 100;
        }
        if ((flags & XingHeaderOptions.VbrScale) != 0)
        {
            xingHeader.vbrScale = ReadBigEndian(rawData, offset);
            offset += 4;
        }
        xingHeader.endOffset = offset;
        return xingHeader;
    }

    /// <summary>
    /// Sees if a frame contains a Xing header
    /// </summary>
    private XingHeader(Mp3Frame frame, byte[] rawData)
    {
        this.frame = frame;
        this.rawData = rawData;
    }

    /// <summary>
    /// Number of frames
    /// </summary>
    public int Frames
    {
        get
        {
            if (framesOffset == -1)
                return -1;
            return ReadBigEndian(rawData, framesOffset);
        }
        set
        {
            if (framesOffset == -1)
                throw new InvalidOperationException("Frames flag is not set");
            WriteBigEndian(rawData, framesOffset, value);
        }
    }

    /// <summary>
    /// Number of bytes
    /// </summary>
    public int Bytes
    {
        get
        {
            if (bytesOffset == -1)
                return -1;
            return ReadBigEndian(rawData, bytesOffset);
        }
        set
        {
            if (framesOffset == -1)
                throw new InvalidOperationException("Bytes flag is not set");
            WriteBigEndian(rawData, bytesOffset, value);
        }
    }

    /// <summary>
    /// VBR Scale property
    /// </summary>
    public int VbrScale
    {
        get { return vbrScale; }
    }

    /// <summary>
    /// The MP3 frame
    /// </summary>
    public Mp3Frame Mp3Frame
    {
        get { return frame; }
    }

}
