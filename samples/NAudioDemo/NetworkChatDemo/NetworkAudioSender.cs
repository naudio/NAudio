using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NAudioDemo.NetworkChatDemo;

/// <summary>
/// Captures microphone audio with <see cref="WasapiRecorder"/> (NAudio 3's recommended WASAPI
/// capture device), encodes it with the chosen codec and pushes each encoded chunk to the network.
/// WASAPI shared mode converts the device's mix format to the codec's requested record format, so
/// any codec sample rate (8/16/48 kHz) works without us doing the resampling.
/// </summary>
internal class NetworkAudioSender : IDisposable
{
    private readonly INetworkChatCodec codec;
    private readonly IAudioSender audioSender;
    private readonly WasapiRecorder waveIn;
    private byte[] captureBuffer = [];

    public NetworkAudioSender(INetworkChatCodec codec, MMDevice inputDevice, IAudioSender audioSender)
    {
        this.codec = codec;
        this.audioSender = audioSender;

        var builder = new WasapiRecorderBuilder()
            .WithFormat(codec.RecordFormat)
            .WithBufferLength(50);
        if (inputDevice != null)
        {
            builder.WithDevice(inputDevice);
        }
        waveIn = builder.Build();
        waveIn.DataAvailable += OnAudioCaptured;
        waveIn.StartRecording();
    }

    private void OnAudioCaptured(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        // The span is only valid for the duration of this callback and the codec API works on a
        // byte[], so copy into a reusable buffer (grown on demand) before encoding.
        if (captureBuffer.Length < buffer.Length)
        {
            captureBuffer = new byte[buffer.Length];
        }
        buffer.CopyTo(captureBuffer);

        byte[] encoded = codec.Encode(captureBuffer, 0, buffer.Length);
        if (encoded.Length > 0)
        {
            audioSender.Send(encoded);
        }
    }

    public void Dispose()
    {
        waveIn.DataAvailable -= OnAudioCaptured;
        waveIn.StopRecording();
        waveIn.Dispose();
        audioSender?.Dispose();
    }
}
