using System;
using NAudio.Wave;

namespace NAudio.Vst3;

/// <summary>
/// Exposes a VST 3® <b>instrument</b> (VSTi) as a continuous <see cref="ISampleProvider"/>: each
/// <see cref="Read"/> pulls the synth's output by driving <see cref="Vst3Plugin.Process"/>, with
/// notes supplied out-of-band via <see cref="Vst3Plugin.SendNoteOn"/> / <c>SendNoteOff</c> (e.g.
/// from a live MIDI input). Unlike <see cref="Vst3EffectSampleProvider"/>, this source never ends —
/// it keeps producing audio (silence when no notes are sounding), which is what a realtime output
/// such as <c>WasapiPlayer</c> expects.
/// </summary>
/// <remarks>
/// <para>
/// Most instruments take no audio input; this provider feeds their (often present but ignored)
/// input bus silence. Instruments that genuinely consume audio — vocoders, or synths that mix an
/// incoming signal with their own output — can be driven by passing an <c>audioInput</c>
/// source to the constructor, whose format must match the plug-in's negotiated input.
/// </para>
/// <para>
/// All <see cref="Read"/> calls happen on the audio render thread. Notes may be sent from any
/// thread — <see cref="Vst3Plugin"/> marshals them across via a lock-free queue.
/// </para>
/// </remarks>
public sealed class Vst3InstrumentSampleProvider : ISampleProvider
{
    private readonly Vst3Plugin _plugin;
    private readonly ISampleProvider? _audioInput;
    private readonly int _outputChannels;
    private readonly int _inputChannels;
    private readonly int _maxBlockSize;
    private readonly float[] _inputBlock;
    private readonly float[] _outputBlock;

    /// <summary>
    /// Wraps an instrument plug-in as a continuous sample source.
    /// </summary>
    /// <param name="plugin">An instrument plug-in (see <see cref="Vst3Plugin.IsInstrument"/>).</param>
    /// <param name="audioInput">
    /// Optional audio fed to the instrument's input bus (for vocoders / audio-consuming synths). When
    /// <c>null</c> the input bus, if present, receives silence. Must match the plug-in's negotiated
    /// input channel count and sample rate.
    /// </param>
    public Vst3InstrumentSampleProvider(Vst3Plugin plugin, ISampleProvider? audioInput = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (!plugin.IsInstrument)
        {
            throw new ArgumentException("Plug-in is not an instrument (no event input bus).", nameof(plugin));
        }

        _plugin = plugin;
        _outputChannels = plugin.OutputChannelCount;
        _inputChannels = plugin.InputChannelCount;
        _maxBlockSize = plugin.MaxBlockSize;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(plugin.SampleRate, _outputChannels);
        _outputBlock = new float[_maxBlockSize * _outputChannels];
        _inputBlock = new float[_maxBlockSize * Math.Max(1, _inputChannels)];

        if (audioInput is not null)
        {
            if (_inputChannels == 0)
            {
                throw new ArgumentException(
                    "Instrument has no audio input bus; cannot route an audio input to it.", nameof(audioInput));
            }
            if (audioInput.WaveFormat.Channels != _inputChannels
                || audioInput.WaveFormat.SampleRate != plugin.SampleRate)
            {
                throw new ArgumentException(
                    $"Audio input must be {_inputChannels}ch @ {plugin.SampleRate} Hz to match the instrument.",
                    nameof(audioInput));
            }
            _audioInput = audioInput;
        }
    }

    /// <inheritdoc/>
    public WaveFormat WaveFormat { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="buffer"/>'s length must be a whole number of frames (a multiple of
    /// <see cref="WaveFormat"/>'s channel count). A non-frame-aligned length silently drops the
    /// trailing partial frame.
    /// </remarks>
    public int Read(Span<float> buffer)
    {
        var count = buffer.Length;
        var produced = 0;
        while (produced < count)
        {
            var frames = Math.Min(_maxBlockSize, (count - produced) / _outputChannels);
            if (frames == 0)
            {
                break; // remaining < one frame (count is normally channel-aligned)
            }

            ReadOnlySpan<float> inputSpan;
            if (_inputChannels > 0)
            {
                var inputCount = frames * _inputChannels;
                if (_audioInput is not null)
                {
                    var got = _audioInput.Read(_inputBlock.AsSpan(0, inputCount));
                    if (got < inputCount)
                    {
                        Array.Clear(_inputBlock, got, inputCount - got); // zero-pad past input EOF
                    }
                }
                else
                {
                    Array.Clear(_inputBlock, 0, inputCount);
                }
                inputSpan = _inputBlock.AsSpan(0, inputCount);
            }
            else
            {
                inputSpan = ReadOnlySpan<float>.Empty;
            }

            var outputCount = frames * _outputChannels;
            var outputSpan = _outputBlock.AsSpan(0, outputCount);
            outputSpan.Clear();
            _plugin.Process(inputSpan, outputSpan, frames);
            outputSpan.CopyTo(buffer.Slice(produced, outputCount));
            produced += outputCount;
        }
        return produced;
    }
}
