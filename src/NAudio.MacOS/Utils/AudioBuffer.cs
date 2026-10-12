using System;

namespace NAudio.Utils;

/// <summary>
/// A very simple audio buffer for retaining audio data. <br />
/// May be moved to the Core library at some time
/// </summary>
internal sealed class AudioBuffer
{
    private byte[] buffer;
    private int offset, count;

    /// <summary>
    /// Creates a new empty buffer.
    /// </summary>
    public AudioBuffer()
    {
        count = 0;
        offset = 0;
        buffer = [];
    }

    /// <summary>
    /// Number of valid bytes contained in this buffer
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Removes all the data contained in this buffer.
    /// </summary>
    public void Clear() => offset = count = 0;

    /// <summary>
    /// Ensures that we can copy <paramref name="count"/> bytes to this
    /// buffer before doing any <see cref="PutBytes(ReadOnlySpan{byte})"/> operation.
    /// </summary>
    /// <param name="count"># of bytes expected to be copied</param>
    /// <exception cref="OverflowException">The buffer reached it's maximum length.</exception>
    public void EnsureBytes(int count)
    {
        int newMaxPos = unchecked(offset + this.count + count);
        if (newMaxPos < 0)
        {
            throw new OverflowException();
        }
        else if (newMaxPos > Array.MaxLength)
        {
            throw new OverflowException("The audio buffer cannot be enlarged past to the maximum array length!");
        }
        else if (newMaxPos > buffer.Length)
        {
            // We do not care if the buffer creation below contains garbage.
            // What we care, though, is that the valid data that it contains are copied safely to the resized buffer.
            byte[] temp = GC.AllocateUninitializedArray<byte>(newMaxPos);
            if (this.count > 0)
            {
                if (offset > 100)
                {
                    // Move the valid bytes to the beginning of the buffer
                    Array.Copy(buffer, offset, temp, 0, this.count);
                    offset = 0;
                }
                else
                {
                    Array.Copy(buffer, offset, temp, offset, this.count);
                }
            }
            buffer = temp;
        }
    }

    /// <summary>Adds audio data to this buffer.</summary>
    /// <param name="bytes">The buffer that contains the data to add.</param>
    public void PutBytes(ReadOnlySpan<byte> bytes)
    {
        int length = bytes.Length;
        bytes.CopyTo(buffer.AsSpan(offset + count, length));
        count += length;
    }

    /// <summary>Retrieves audio data from the buffer.</summary>
    /// <param name="target">The buffer where the data should be copied into</param>
    /// <returns># of bytes copied to <paramref name="target"/>. 0 if no bytes are contained in the buffer.</returns>
    public int GetBytes(Span<byte> target)
    {
        int copyCount = Math.Min(count, target.Length);
        if (copyCount > 0)
        {
            buffer.AsSpan(offset, copyCount).CopyTo(target);
            offset += copyCount;
            count -= copyCount;
        }
        // Reset offset & count if the buffer was drained
        if (count < 1) { offset = count = 0; }
        return copyCount;
    }
}
