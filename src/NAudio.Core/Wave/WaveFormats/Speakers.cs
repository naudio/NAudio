using System;

namespace NAudio.Wave;

/// <summary>
/// Speaker position flags used to build the <c>dwChannelMask</c> of a
/// <see cref="WaveFormatExtensible"/>. The values match the <c>SPEAKER_*</c>
/// constants from the Windows <c>ksmedia.h</c> header, so a combination of
/// these flags can be passed directly as a channel mask.
/// </summary>
[Flags]
public enum Speakers
{
    /// <summary>No speaker positions specified.</summary>
    None = 0x0,
    /// <summary>Front left (SPEAKER_FRONT_LEFT).</summary>
    FrontLeft = 0x1,
    /// <summary>Front right (SPEAKER_FRONT_RIGHT).</summary>
    FrontRight = 0x2,
    /// <summary>Front center (SPEAKER_FRONT_CENTER).</summary>
    FrontCenter = 0x4,
    /// <summary>Low frequency / subwoofer (SPEAKER_LOW_FREQUENCY).</summary>
    LowFrequency = 0x8,
    /// <summary>Back left (SPEAKER_BACK_LEFT).</summary>
    BackLeft = 0x10,
    /// <summary>Back right (SPEAKER_BACK_RIGHT).</summary>
    BackRight = 0x20,
    /// <summary>Front left of center (SPEAKER_FRONT_LEFT_OF_CENTER).</summary>
    FrontLeftOfCenter = 0x40,
    /// <summary>Front right of center (SPEAKER_FRONT_RIGHT_OF_CENTER).</summary>
    FrontRightOfCenter = 0x80,
    /// <summary>Back center (SPEAKER_BACK_CENTER).</summary>
    BackCenter = 0x100,
    /// <summary>Side left (SPEAKER_SIDE_LEFT).</summary>
    SideLeft = 0x200,
    /// <summary>Side right (SPEAKER_SIDE_RIGHT).</summary>
    SideRight = 0x400,
    /// <summary>Top center (SPEAKER_TOP_CENTER).</summary>
    TopCenter = 0x800,
    /// <summary>Top front left (SPEAKER_TOP_FRONT_LEFT).</summary>
    TopFrontLeft = 0x1000,
    /// <summary>Top front center (SPEAKER_TOP_FRONT_CENTER).</summary>
    TopFrontCenter = 0x2000,
    /// <summary>Top front right (SPEAKER_TOP_FRONT_RIGHT).</summary>
    TopFrontRight = 0x4000,
    /// <summary>Top back left (SPEAKER_TOP_BACK_LEFT).</summary>
    TopBackLeft = 0x8000,
    /// <summary>Top back center (SPEAKER_TOP_BACK_CENTER).</summary>
    TopBackCenter = 0x10000,
    /// <summary>Top back right (SPEAKER_TOP_BACK_RIGHT).</summary>
    TopBackRight = 0x20000,

    /// <summary>Mono (front center).</summary>
    Mono = FrontCenter,
    /// <summary>Stereo (front left and right).</summary>
    Stereo = FrontLeft | FrontRight,
    /// <summary>Quadraphonic (front and back, left and right).</summary>
    Quad = FrontLeft | FrontRight | BackLeft | BackRight,
    /// <summary>5.1 surround (front L/R/C, LFE, side L/R).</summary>
    Surround51 = FrontLeft | FrontRight | FrontCenter | LowFrequency | SideLeft | SideRight,
    /// <summary>7.1 surround (5.1 plus back L/R).</summary>
    Surround71 = Surround51 | BackLeft | BackRight,
}
