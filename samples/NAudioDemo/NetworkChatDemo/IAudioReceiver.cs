using System;

namespace NAudioDemo.NetworkChatDemo;

internal interface IAudioReceiver : IDisposable
{
    void OnReceived(Action<byte[]> handler);
}
