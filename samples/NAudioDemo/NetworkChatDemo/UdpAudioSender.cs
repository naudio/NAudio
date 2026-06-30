using System.Net.Sockets;

namespace NAudioDemo.NetworkChatDemo;

/// <summary>
/// Sends each encoded audio chunk as a single UDP datagram. UDP is the recommended
/// transport for real-time audio: a lost or late datagram causes a small glitch rather
/// than stalling the whole stream the way a missing TCP segment would (head-of-line
/// blocking). Each datagram carries exactly one encoder output, so no framing is needed.
/// </summary>
internal class UdpAudioSender : IAudioSender
{
    private readonly UdpClient udpSender;

    public UdpAudioSender(string host, int port)
    {
        udpSender = new UdpClient();
        // Connect resolves host names (so you can type a machine name, not just an IP)
        // and fixes the default destination, so subsequent Send calls need no endpoint.
        udpSender.Connect(host, port);
    }

    public void Send(byte[] payload)
    {
        udpSender.Send(payload, payload.Length);
    }

    public void Dispose()
    {
        udpSender.Dispose();
    }
}
