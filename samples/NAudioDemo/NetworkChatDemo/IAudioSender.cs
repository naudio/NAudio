using System;

namespace NAudioDemo.NetworkChatDemo;

internal interface IAudioSender : IDisposable
{
    void Send(byte[] payload);
}
