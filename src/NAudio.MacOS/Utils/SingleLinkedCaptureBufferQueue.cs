
using System;
using System.Threading;

using NAudio.Wave;

namespace NAudio.Utils;

/// <summary>
/// Provides a queue for capture buffers returned from CoreAudioRecorder instances. <br />
/// This queue serializes access to enqueues/dequeues.
/// </summary>
internal sealed class SingleLinkedCaptureBufferQueue : IDisposable
{
    private sealed class Node
    {
        public Node Next;
        public readonly CaptureBuffer Buffer;

        public Node(CaptureBuffer buffer)
        {
            Next = null;
            Buffer = buffer;
        }
    }

    private Node root, current;
    private readonly SemaphoreSlim lockSemaphore;

    public SingleLinkedCaptureBufferQueue()
    {
        root = current = null;
        lockSemaphore = new(1);
    }

    public CaptureBuffer Dequeue()
    {
        // Make the check whether we have a buffer to dequeue
        // outside the critical region to avoid paying additional
        // overhead due to locking on the object; 
        // Checking against the root field is safe because we only query
        // by it and the updates done to this field are safely guarded.
        if (root is null) { return null; }
        lockSemaphore.Wait();
        try
        {
            var cb = root.Buffer;
            root = root.Next;
            return cb;
        }
        finally
        {
            _ = lockSemaphore.Release();
        }
    }

    public void Enqueue(CaptureBuffer buffer)
    {
        Node newNode = new(buffer);
        lockSemaphore.Wait();
        try
        {
            if (root is null)
            {
                root = current = newNode;
            }
            else
            {
                current.Next = newNode;
                current = newNode;
            }
        }
        finally
        {
            _ = lockSemaphore.Release();
        }
    }

    public void EnqueueFromHandler(
        ReadOnlySpan<byte> audioData,
        double currentTime,
        double firstByteFromHAL
    )
    {
        Enqueue(new(
            audioData.ToArray(),
            currentTime,
            firstByteFromHAL
        ));
    }

    public void Dispose() => lockSemaphore.Dispose();
}