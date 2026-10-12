using System;
using System.Runtime.CompilerServices;

namespace NAudio.Utils;

/// <summary>
/// A very simple channel interleaver - interleaves any samples from any buffers and returns a single,
/// interleaved buffer.
/// It is used for cases where the HAL is in non-interleaved mode, where each buffer is a single channel of data.
/// </summary>
internal sealed class AudioInterleaver
{
    private int blockAlign;
    private int totalBytes;
    private int minSingleBuffer;
    private byte[] buildingBuffer;
    private AudioBuffer[] remainderSamples;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioInterleaver"/> class.
    /// </summary>
    public AudioInterleaver()
    {
        blockAlign = 0;
        totalBytes = 0;
        buildingBuffer = [];
        minSingleBuffer = 0;
        remainderSamples = [];
    }

    /// <summary>
    /// Container bytes per sample of the non-interleaved format.
    /// </summary>
    public int BytesPerFrame
    {
        get => blockAlign;
        set => blockAlign = value;
    }

    /// <summary>
    /// Number of channels.
    /// </summary>
    public int Channels
    {
        get => remainderSamples.Length;
        set
        {
            if (value != remainderSamples.Length)
            {
                remainderSamples = new AudioBuffer[value];
            }
        }
    }

    /// <summary>
    /// Clears any buffer-specific context to be prepared for a new buffer
    /// </summary>
    public void PrepareNewBuffer()
    {
        totalBytes = 0;
        minSingleBuffer = int.MaxValue;
    }

    /// <summary>
    /// Given a channel buffer, it ensures that the interleaver can interleave it's data.
    /// </summary>
    /// <param name="channelOrdinal">The channel index.</param>
    /// <param name="bytes">The number of bytes held by the buffer.</param>
    public void AddBufferBytes(int channelOrdinal, int bytes)
    {
        (remainderSamples[channelOrdinal] ??= new()).EnsureBytes(bytes);
        if (bytes < minSingleBuffer) { minSingleBuffer = bytes; }
    }

    /// <summary>
    /// From a series of <see cref="AddBufferBytes(int, int)"/> calls,
    /// it builds the final, interleaved buffer where audio data will be finally
    /// provided by <see cref="PutBytes(int, ReadOnlySpan{byte})"/> calls. <br />
    /// After all the buffers have been supplied, <see cref="GetBuffer"/> can be used to obtain the buffer.
    /// </summary>
    public void BuildNewBuffer()
    {
        // We could create an inconsistent buffer.
        // To avoid this issue, we will use the minimum bytes of all AddBufferBytes calls.
        // The remainderSamples buffers will then handle any extraneous samples.
        totalBytes = minSingleBuffer * remainderSamples.Length;
        SetBuildingBufferCapacity(totalBytes);
    }

    /// <summary>
    /// Makes sure that the buffer that is built through 
    /// <see cref="BuildNewBuffer"/> and <see cref="AddBufferBytes(int, int)"/> has enough space.
    /// </summary>
    /// <param name="newCapacity"># of bytes to ensure.</param>
    public void SetBuildingBufferCapacity(int newCapacity)
    {
        if (buildingBuffer is null || newCapacity > buildingBuffer.Length)
        {
            buildingBuffer = GC.AllocateUninitializedArray<byte>(newCapacity);
        }
    }

    /// <summary>
    /// Provides bytes from a single-channel, non-interleaved buffer.
    /// </summary>
    /// <param name="channelOrdinal">The channel index.</param>
    /// <param name="source">The buffer's contents.</param>
    public void PutBytes(int channelOrdinal, ReadOnlySpan<byte> source)
    {
        int bytesCopied = 0;
        int buildingBufferCopied = 0;
        int bufferIndex = channelOrdinal * blockAlign;
        int bytesToCopy = Math.Min(source.Length, minSingleBuffer);
        int buildingBufferBlockAlign = remainderSamples.Length * blockAlign;

        var remainderBuffer = remainderSamples[channelOrdinal];

        // If the remainder buffer has data, copy them to the building buffer.
        // Only accept valid samples, drop samples that are malformed.
        while (remainderBuffer.Count >= blockAlign && buildingBufferCopied < minSingleBuffer)
        {
            _ = remainderBuffer.GetBytes(buildingBuffer.AsSpan(bufferIndex, blockAlign));
            bufferIndex += buildingBufferBlockAlign;
            buildingBufferCopied += blockAlign;
        }

        for (
             ;
             bytesCopied < bytesToCopy && buildingBufferCopied < minSingleBuffer;
             bufferIndex += buildingBufferBlockAlign,
             bytesCopied += blockAlign,
             buildingBufferCopied += blockAlign
        )
        {
            // Copy bytes from the provided buffer to the building buffer.
            source.Slice(bytesCopied, blockAlign).CopyTo(buildingBuffer.AsSpan(bufferIndex, blockAlign));
        }

        if (bytesCopied < source.Length)
        {
            ReadOnlySpan<byte> remaining = source.Slice(bytesCopied);
            remainderBuffer.EnsureBytes(remaining.Length);
            remainderBuffer.PutBytes(remaining);
        }
    }

    /// <summary>
    /// Flushes any stashed samples due to buffer inconsistencies. <br />
    /// Call this method repeatedly to do so. Example: <br />
    /// <code>
    /// // At the top...
    /// using NAudio.Utils;
    /// 
    /// AudioInterleaver interleaver;
    /// 
    /// // No new buffers reported from the source...
    /// while (interleaver.Drain())
    /// {
    ///     _ = interleaver.GetBuffer(); // Write the data somewhere.
    /// }
    /// </code>
    /// </summary>
    /// <returns>A value whether the <see cref="GetBuffer"/> contains valid audio data.</returns>
    public bool Drain()
    {
        PrepareNewBuffer();

        int maxSingleBuffer = 0;

        foreach (var buffer in remainderSamples)
        {
            int count = buffer.Count;
            if (count > maxSingleBuffer) { maxSingleBuffer = count; }
        }

        totalBytes = maxSingleBuffer * remainderSamples.Length;
        SetBuildingBufferCapacity(totalBytes);

        if (totalBytes > 0)
        {
            // Since we are now draining the buffers, extra care must be taken
            // to ensure that the result buffer is zeroized.
            // We need it zeroized because we can have some buffers drained,
            // while some others not. So, instead of hearing whatever existed
            // before for the buffer(s) we won't write, silence will be instead heard.
            Unsafe.InitBlockUnaligned(ref buildingBuffer[0], 0, (uint)totalBytes);

            int channel = 0;
            int buildingBufferBlockAlign = remainderSamples.Length * blockAlign;

            foreach (var buffer in remainderSamples)
            {
                int bytesCopied = 0;
                int buildingBufferCopied = 0;
                int bufferIndex = (channel++) * blockAlign;
                int bytesToCopy = Math.Min(buffer.Count, maxSingleBuffer);

                for (
                     ;
                     bytesCopied < bytesToCopy && buildingBufferCopied < maxSingleBuffer;
                     bufferIndex += buildingBufferBlockAlign,
                     bytesCopied += blockAlign,
                     buildingBufferCopied += blockAlign
                )
                {
                    // Copy bytes from the internal buffer to the building buffer.
                    _ = buffer.GetBytes(buildingBuffer.AsSpan(bufferIndex, blockAlign));
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the final interleaved buffer built by all the provided non-interleaved buffers.
    /// </summary>
    /// <returns>The interleaved buffer.</returns>
    public ReadOnlySpan<byte> GetBuffer() => buildingBuffer.AsSpan(0, totalBytes);
}
