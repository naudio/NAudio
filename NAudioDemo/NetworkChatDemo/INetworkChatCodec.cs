using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo
{
    interface INetworkChatCodec : IDisposable
    {
        string Name { get; }
        WaveFormat RecordFormat { get; }
        byte[] Encode(byte[] data, int offset, int length);
        byte[] Decode(byte[] data);
    }
}
