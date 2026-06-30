using System;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo;

/// <summary>
/// Decodes incoming network audio and plays it out through the default output device using
/// <see cref="WasapiPlayer"/> (NAudio 3's recommended WASAPI playback device). Received chunks are
/// pushed into a <see cref="BufferedWaveProvider"/> that acts as a small jitter buffer, smoothing
/// out the uneven arrival times you get over a real network. WASAPI shared mode resamples the
/// codec's format to the device mix format automatically.
/// </summary>
internal class NetworkAudioPlayer : IDisposable
{
    private readonly INetworkChatCodec codec;
    private readonly IAudioReceiver receiver;
    private readonly IWavePlayer waveOut;
    private readonly BufferedWaveProvider waveProvider;

    public NetworkAudioPlayer(INetworkChatCodec codec, IAudioReceiver receiver)
    {
        this.codec = codec;
        this.receiver = receiver;
        receiver.OnReceived(OnDataReceived);

        // Keep the playback latency low - this is real-time chat, not file playback.
        waveOut = new WasapiPlayerBuilder()
            .WithLatency(100)
            .Build();
        // Keep at most ~500ms queued. DiscardOnBufferOverflow drops the oldest audio if the network
        // briefly delivers faster than we play, which caps end-to-end latency instead of letting it
        // grow unbounded (and avoids the buffer-full exception the old code could throw). Underruns
        // simply play silence until the next chunk arrives.
        waveProvider = new BufferedWaveProvider(codec.RecordFormat, TimeSpan.FromMilliseconds(500))
        {
            DiscardOnBufferOverflow = true
        };
        waveOut.Init(waveProvider);
        waveOut.Play();
    }

    private void OnDataReceived(byte[] encoded)
    {
        byte[] decoded = codec.Decode(encoded, 0, encoded.Length);
        waveProvider.AddSamples(decoded, 0, decoded.Length);
    }

    public void Dispose()
    {
        receiver?.Dispose();
        waveOut?.Dispose();
    }
}
