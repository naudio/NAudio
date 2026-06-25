namespace NAudio.Sampler;

/// <summary>
/// When a region triggers a voice (SFZ <c>trigger</c>). SoundFont regions are
/// always <see cref="Attack"/>.
/// </summary>
public enum SamplerTrigger
{
    /// <summary>Sound on note-on (the default).</summary>
    Attack,
    /// <summary>Sound on note-off.</summary>
    Release,
    /// <summary>Sound on note-on only when no other notes are held on the channel.</summary>
    First,
    /// <summary>Sound on note-on only when other notes are already held (legato).</summary>
    Legato
}
