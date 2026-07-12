using System;
using NAudio.Dsp;

namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Applies an ADSR (attack / decay / sustain / release) amplitude envelope to a source
/// sample provider. Wraps <see cref="EnvelopeGenerator"/>, exposing its stage times in
/// seconds and its sustain level, and multiplies the source by the envelope each sample.
/// </summary>
/// <remarks>
/// The envelope holds at <see cref="SustainLevel"/> once the attack and decay have
/// completed, until <see cref="Stop"/> is called to enter the release phase. Once the
/// release finishes the envelope is idle, <see cref="Read"/> returns 0 (so the provider
/// self-evicts from a <see cref="MixingSampleProvider"/>), and <see cref="EnvelopeCompleted"/>
/// is raised.
/// </remarks>
public class AdsrSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly EnvelopeGenerator adsr;
    private readonly int channels;
    private readonly float sampleRate;
    private float attackSeconds;
    private float decaySeconds;
    private float releaseSeconds;
    private float sustainLevel;
    private bool raisedCompleted;

    /// <summary>
    /// Creates a new AdsrSampleProvider with default values (fast attack, no decay,
    /// full sustain, 0.3s release).
    /// </summary>
    /// <param name="source">The source to apply the envelope to. Any channel count is supported.</param>
    /// <param name="startGateOpen">
    /// When true (the default) the envelope's gate opens immediately, so the note begins its
    /// attack as soon as playback starts — matching the previous behaviour of this class. Set
    /// to false to arm the provider without starting it and trigger the note later with
    /// <see cref="Start"/>; note that while idle <see cref="Read"/> returns 0, so do not
    /// initialise a player directly on an un-started provider and treat that as end-of-stream.
    /// </param>
    public AdsrSampleProvider(ISampleProvider source, bool startGateOpen = true)
    {
        this.source = source;
        channels = source.WaveFormat.Channels;
        sampleRate = source.WaveFormat.SampleRate;
        adsr = new EnvelopeGenerator();

        // Defaults reproduce the previous behaviour: fast attack, no decay, full sustain.
        AttackSeconds = 0.01f;
        DecaySeconds = 0.0f;
        SustainLevel = 1.0f;
        ReleaseSeconds = 0.3f;

        if (startGateOpen)
        {
            adsr.Gate(true);
        }
    }

    /// <summary>
    /// Attack time in seconds (time for the envelope to rise from silence to full level).
    /// </summary>
    public float AttackSeconds
    {
        get => attackSeconds;
        set
        {
            attackSeconds = value;
            adsr.AttackRate = SecondsToSamples(value);
        }
    }

    /// <summary>
    /// Decay time in seconds (time for the envelope to fall from full level to the sustain level).
    /// </summary>
    public float DecaySeconds
    {
        get => decaySeconds;
        set
        {
            decaySeconds = value;
            adsr.DecayRate = SecondsToSamples(value);
        }
    }

    /// <summary>
    /// Sustain level in the range 0..1 (the level held after the decay until release).
    /// </summary>
    public float SustainLevel
    {
        get => sustainLevel;
        set
        {
            sustainLevel = value;
            adsr.SustainLevel = value;
        }
    }

    /// <summary>
    /// Release time in seconds (time for the envelope to fall from the sustain level to silence
    /// after <see cref="Stop"/>).
    /// </summary>
    public float ReleaseSeconds
    {
        get => releaseSeconds;
        set
        {
            releaseSeconds = value;
            adsr.ReleaseRate = SecondsToSamples(value);
        }
    }

    /// <summary>
    /// The current envelope phase.
    /// </summary>
    public EnvelopeGenerator.EnvelopeState State => adsr.State;

    /// <summary>
    /// Raised once when the envelope finishes (returns to Idle after the release completes).
    /// Fires on the thread that calls <see cref="Read"/> (i.e. the audio thread during playback),
    /// in the same manner as <see cref="MixingSampleProvider.MixerInputEnded"/>.
    /// </summary>
    public event EventHandler EnvelopeCompleted;

    /// <summary>
    /// Triggers note-on, entering the attack phase. Can be used to retrigger a note that has
    /// already been released.
    /// </summary>
    public void Start()
    {
        raisedCompleted = false;
        adsr.Gate(true);
    }

    /// <summary>
    /// Triggers note-off, entering the release phase.
    /// </summary>
    public void Stop()
    {
        adsr.Gate(false);
    }

    /// <summary>
    /// Reads audio from this sample provider, applying the envelope.
    /// </summary>
    public int Read(Span<float> buffer)
    {
        if (adsr.State == EnvelopeGenerator.EnvelopeState.Idle)
        {
            RaiseCompletedOnce();
            return 0; // finished
        }

        var samplesRead = source.Read(buffer);
        for (int n = 0; n < samplesRead; n += channels)
        {
            // Advance the envelope once per frame and apply the same gain to every channel.
            var gain = adsr.Process();
            for (int c = 0; c < channels; c++)
            {
                buffer[n + c] *= gain;
            }
        }

        if (adsr.State == EnvelopeGenerator.EnvelopeState.Idle)
        {
            RaiseCompletedOnce();
        }
        return samplesRead;
    }

    /// <summary>
    /// The output WaveFormat
    /// </summary>
    public WaveFormat WaveFormat => source.WaveFormat;

    // At least one sample so a zero stage time can't divide by zero in the engine's
    // coefficient calculation.
    private float SecondsToSamples(float seconds) => Math.Max(1f, seconds * sampleRate);

    private void RaiseCompletedOnce()
    {
        if (!raisedCompleted)
        {
            raisedCompleted = true;
            EnvelopeCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
