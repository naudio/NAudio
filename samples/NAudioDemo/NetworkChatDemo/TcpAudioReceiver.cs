using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NAudioDemo.NetworkChatDemo;

/// <summary>
/// Accepts a single TCP connection and reassembles the length-prefixed audio chunks written
/// by <see cref="TcpAudioSender"/>. Binds to <see cref="IPAddress.Any"/> so a peer on another
/// machine can connect (the original demo bound the listener to <see cref="IPAddress.Loopback"/>,
/// which only ever accepted connections from the same PC - see GitHub issue #821).
/// </summary>
internal class TcpAudioReceiver : IAudioReceiver
{
    // Guard against a corrupt/garbage length prefix asking us to allocate something huge.
    private const int MaxChunkBytes = 64 * 1024;

    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellation = new();
    private Action<byte[]> handler;

    public TcpAudioReceiver(int listenPort)
    {
        listener = new TcpListener(IPAddress.Any, listenPort);
        listener.Start();
        _ = AcceptLoopAsync(cancellation.Token);
    }

    public void OnReceived(Action<byte[]> onAudioReceivedAction)
    {
        handler = onAudioReceivedAction;
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(token);
                client.NoDelay = true;
                await ReceiveChunksAsync(client.GetStream(), token);
            }
        }
        catch (OperationCanceledException)
        {
            // expected when we are disposed
        }
        catch (SocketException)
        {
            // usually not a problem - just means we have disconnected
        }
        catch (ObjectDisposedException)
        {
            // listener stopped from under us during shutdown
        }
    }

    private async Task ReceiveChunksAsync(NetworkStream stream, CancellationToken token)
    {
        var lengthPrefix = new byte[4];
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!await ReadExactlyAsync(stream, lengthPrefix, lengthPrefix.Length, token))
                {
                    break; // peer closed the connection
                }
                int chunkLength = BinaryPrimitives.ReadInt32BigEndian(lengthPrefix);
                if (chunkLength <= 0 || chunkLength > MaxChunkBytes)
                {
                    break; // framing is out of sync - drop the connection
                }
                var chunk = new byte[chunkLength];
                if (!await ReadExactlyAsync(stream, chunk, chunkLength, token))
                {
                    break;
                }
                handler?.Invoke(chunk);
            }
        }
        catch (IOException)
        {
            // connection reset - loop back round and wait for a new client
        }
        catch (SocketException)
        {
        }
    }

    private static async Task<bool> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
    {
        int read = 0;
        while (read < count)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(read, count - read), token);
            if (n == 0)
            {
                return false; // end of stream
            }
            read += n;
        }
        return true;
    }

    public void Dispose()
    {
        cancellation.Cancel();
        listener.Stop();
        cancellation.Dispose();
    }
}
