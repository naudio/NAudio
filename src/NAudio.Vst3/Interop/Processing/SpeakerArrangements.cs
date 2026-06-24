namespace NAudio.Vst3.Interop;

/// <summary>
/// VST 3 speaker arrangement bitmasks (<c>Vst::SpeakerArrangement</c> = <c>uint64</c>). Each bit
/// represents one speaker position; an arrangement is the OR of its constituent speakers.
/// </summary>
/// <remarks>
/// Defined in <c>pluginterfaces/vst/vstspeaker.h</c>. Phase 2 only exercises the mono and stereo
/// values; the wider surround set is documented for completeness when later phases need it.
/// </remarks>
internal static class SpeakerArrangements
{
    public const ulong SpeakerL = 1ul << 0;   // Front-Left
    public const ulong SpeakerR = 1ul << 1;   // Front-Right
    public const ulong SpeakerC = 1ul << 2;   // Front-Center
    public const ulong SpeakerLfe = 1ul << 3; // Low Frequency Effects
    public const ulong SpeakerLs = 1ul << 4;  // Surround-Left
    public const ulong SpeakerRs = 1ul << 5;  // Surround-Right

    /// <summary>Empty arrangement — no channels.</summary>
    public const ulong Empty = 0;

    /// <summary>Mono (front-center only).</summary>
    public const ulong Mono = SpeakerC;

    /// <summary>Stereo (front-left + front-right).</summary>
    public const ulong Stereo = SpeakerL | SpeakerR;
}
