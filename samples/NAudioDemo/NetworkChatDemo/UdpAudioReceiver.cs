using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NAudioDemo.NetworkChatDemo;

/// <summary>
/// Listens for incoming audio datagrams on a UDP port. Binds to <see cref="IPAddress.Any"/>
/// so packets arriving from other machines on the network are received, not just loopback
/// traffic (the original demo bound to <see cref="IPAddress.Loopback"/>, which is why audio
/// only ever worked between two instances on the same PC - see GitHub issue #821).
/// </summary>
internal class UdpAudioReceiver : IAudioReceiver
{
    private readonly UdpClient udpListener;
    private readonly CancellationTokenSource cancellation = new();
    private Action<byte[]> handler;

    public UdpAudioReceiver(int listenPort)
    {
        udpListener = new UdpClient();
        // ReuseAddress lets two instances on one machine bind the same port for quick
        // loopback experiments, and avoids "address already in use" when restarting fast.
        udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));
        _ = ReceiveLoopAsync(cancellation.Token);
    }

    public void OnReceived(Action<byte[]> onAudioReceivedAction)
    {
        handler = onAudioReceivedAction;
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = await udpListener.ReceiveAsync(token);
                handler?.Invoke(result.Buffer);
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
            // socket closed from under us during shutdown
        }
    }

    public void Dispose()
    {
        cancellation.Cancel();
        udpListener.Dispose();
        cancellation.Dispose();
    }
}
