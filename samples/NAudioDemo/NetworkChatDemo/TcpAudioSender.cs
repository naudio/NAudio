using System.Buffers.Binary;
using System.Net.Sockets;

namespace NAudioDemo.NetworkChatDemo;

/// <summary>
/// Sends encoded audio over a TCP connection. Because TCP is a byte stream with no message
/// boundaries, each chunk is prefixed with a 4-byte big-endian length so the receiver can
/// reassemble exactly the chunks that were sent (the original demo sent raw bytes, so a chunk
/// could be split or merged in transit and fed to the decoder mis-aligned). TCP guarantees
/// delivery and ordering but a single lost packet stalls everything behind it, so prefer UDP
/// for real-time chat; TCP is mainly useful across links that block or mangle UDP.
/// </summary>
internal class TcpAudioSender : IAudioSender
{
    private readonly TcpClient tcpClient;
    private readonly NetworkStream stream;
    private readonly byte[] lengthPrefix = new byte[4];

    public TcpAudioSender(string host, int port)
    {
        tcpClient = new TcpClient { NoDelay = true };
        tcpClient.Connect(host, port);
        stream = tcpClient.GetStream();
    }

    public void Send(byte[] payload)
    {
        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, payload.Length);
        stream.Write(lengthPrefix, 0, lengthPrefix.Length);
        stream.Write(payload, 0, payload.Length);
    }

    public void Dispose()
    {
        stream?.Dispose();
        tcpClient?.Dispose();
    }
}
