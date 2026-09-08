// This interop definition was derived from the file AudioHardwareBase.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System.Numerics;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioObjectID <br />
/// A structure that provides a handle on a specific AudioObject.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
internal readonly struct AudioObjectID : IEqualityOperators<AudioObjectID, AudioObjectID, bool>
{
    private const uint kAudioObjectUnknown = 0U;
    private const uint kAudioObjectSystemObject = 1U;

    /// <summary>
    /// This is the sentinel value. No object will have an ID whose value is 0.
    /// </summary>
    public static readonly AudioObjectID Unknown = new(kAudioObjectUnknown);

    /// <summary>
    /// The <see cref="AudioObjectID"/> that always refers to the one and only instance of the AudioSystemObject class.
    /// </summary>
    public static readonly AudioObjectID SystemObject = new(kAudioObjectSystemObject);

    private readonly uint Value;

    private AudioObjectID(uint value) => Value = value;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object obj) => obj is AudioObjectID aid && aid.Value == Value;

    public static bool IsUnknownID(AudioObjectID id) => id.Value == kAudioObjectUnknown;

    public static bool IsSystemObjectID(AudioObjectID id) => id.Value == kAudioObjectSystemObject;

    public static bool operator ==(AudioObjectID left, AudioObjectID right) => left.Value == right.Value;

    public static bool operator !=(AudioObjectID left, AudioObjectID right) => left.Value != right.Value;
}