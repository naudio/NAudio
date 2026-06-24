using System;

namespace NAudioDemo.NetworkChatDemo
{
    interface IAudioReceiver : IDisposable
    {
        void OnReceived(Action<byte[]> handler);
    }
}