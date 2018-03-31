using System;

namespace NAudioDemo.NetworkChatDemo
{
    interface IAudioSender : IDisposable
    {
        void Send(byte[] payload);
    }
}