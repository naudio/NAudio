using System;

namespace NAudio.Dsp;

/// <summary>
/// Energy-based voice activity detector with an adaptive noise floor and hangover.
/// Operates on a mono sample stream in fixed analysis frames: a frame counts as
/// speech when its level rises a configurable margin above the tracked noise floor,
/// and the decision is held for a short hangover so word-final sounds are not
/// clipped. Used to gate noise-estimate updates (noise suppression), freeze AGC
/// during silence, or drive a transmit gate.
/// </summary>
public sealed class VoiceActivityDetector
{
    private readonly int frameSamples;
    private readonly float frameMs;
    private double sumSquares;
    private int frameFill;
    private float noiseFloorDb = -90f;
    private bool initialised;
    private int hangoverFrames;
    private int hangoverRemaining;
    private bool active;

    /// <summary>
    /// Creates a detector.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz. Must be positive.</param>
    /// <param name="frameMilliseconds">Analysis frame length in ms. Default 20 ms.</param>
    public VoiceActivityDetector(int sampleRate, float frameMilliseconds = 20f)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
        if (frameMilliseconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(frameMilliseconds), "Frame length must be positive");
        frameSamples = Math.Max(1, (int)(frameMilliseconds * 0.001f * sampleRate));
        frameMs = frameSamples * 1000f / sampleRate;
        hangoverFrames = Math.Max(1, (int)(200f / frameMs));
    }

    /// <summary>Margin in dB above the noise floor that counts as speech. Default 9 dB.</summary>
    public float ThresholdDb { get; set; } = 9f;

    /// <summary>Hangover time in milliseconds (decision held after speech ends). Default 200 ms.</summary>
    public float HangoverMs
    {
        get => hangoverFrames * frameMs;
        set => hangoverFrames = Math.Max(1, (int)MathF.Round(value / frameMs));
    }

    /// <summary>The current adaptive noise floor estimate in dBFS.</summary>
    public float NoiseFloorDb => noiseFloorDb;

    /// <summary>True if the most recent frame (including hangover) is classed as speech.</summary>
    public bool IsVoiceActive => active;

    /// <summary>
    /// Feeds one mono sample and returns the current voice-activity decision.
    /// </summary>
    public bool Process(float sample)
    {
        sumSquares += sample * (double)sample;
        if (++frameFill < frameSamples)
            return active;

        var rms = MathF.Sqrt((float)(sumSquares / frameSamples));
        var frameDb = 20f * MathF.Log10(rms < 1e-9f ? 1e-9f : rms);
        sumSquares = 0;
        frameFill = 0;

        if (!initialised)
        {
            noiseFloorDb = frameDb;
            initialised = true;
        }
        else if (frameDb < noiseFloorDb)
        {
            // Track quiet quickly, rise slowly so speech doesn't inflate the floor.
            noiseFloorDb += 0.5f * (frameDb - noiseFloorDb);
        }
        else
        {
            noiseFloorDb += 0.001f * (frameDb - noiseFloorDb);
        }

        var speech = frameDb > noiseFloorDb + ThresholdDb;
        if (speech)
            hangoverRemaining = hangoverFrames;
        else if (hangoverRemaining > 0)
        {
            hangoverRemaining--;
            speech = true;
        }

        active = speech;
        return active;
    }

    /// <summary>
    /// Resets the detector to its initial (un-adapted) state.
    /// </summary>
    public void Reset()
    {
        sumSquares = 0;
        frameFill = 0;
        noiseFloorDb = -90f;
        initialised = false;
        hangoverRemaining = 0;
        active = false;
    }
}
