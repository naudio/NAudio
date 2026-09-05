// This interop definition was derived from the file AudioHardwareBase.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System;
using NAudio.Wave;
using NAudio.Utils;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// AudioStream is a subclass of AudioObject and has only the single scope,
/// kAudioObjectPropertyScopeGlobal. They have a main element and an element for
/// each channel in the stream numbered upward from 1.
/// </summary>
public sealed class AudioStream : AudioObject
{
    internal AudioStream(AudioObjectID objectID) : base(objectID) { }

    /// <summary>
    /// A <see cref="bool"/> where a <see langword="true"/> value indicates that the stream is enabled and doing IO.
    /// </summary>
    public bool IsActive => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyIsActive)) == 1U;

    /// <summary>
    /// Returns a <see cref="AudioStreamDirection"/> value indicating whether this 
    /// <see cref="AudioStream"/> is an output or input stream.
    /// </summary>
    /// <seealso cref="AudioStreamDirection"/>
    public AudioStreamDirection Direction => (AudioStreamDirection)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyDirection));

    /// <summary>
    /// A <see cref="uint"/> that specifies the first element in the owning 
    /// device that corresponds to element one of this stream.
    /// </summary>
    public uint StartingChannel => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyStartingChannel));

    /// <summary>
    /// A <see cref="AudioStreamTerminalType"/> whose value describes the general kind of functionality attached to the AudioStream.
    /// </summary>
    /// <seealso cref="AudioStreamTerminalType"/>
    public AudioStreamTerminalType TerminalType => (AudioStreamTerminalType)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyTerminalType));

    /// <summary>
    /// A <see cref="uint"/> containing the number of frames of latency in the AudioStream. Note
    /// that the owning AudioDevice may have additional latency so it should be
    /// queried as well. If both the device and the stream say they have latency,
    /// then the total latency for the stream is the device latency summed with the
    /// stream latency.
    /// </summary>
    public uint Latency => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyLatency));

    /// <summary>
    /// A <see cref="WaveFormat"/> that describes the current data format for
    /// the <see cref="AudioStream"/>. The virtual format refers to the data format in which all
    /// I/O procedures for the owning <see cref="AudioDevice"/> will perform I/O transactions.
    /// </summary>
    public WaveFormat VirtualFormat
    {
        get => MacUtils.ConstructWaveFormatFromASBD(GetAudioStreamBasicDescriptionValue(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioStreamProperties.kAudioStreamPropertyVirtualFormat
            )
        ));
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetAudioStreamBasicDescriptionValue(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioStreamProperties.kAudioStreamPropertyVirtualFormat
                ),
                MacUtils.ConstructASBDFromWaveFormat(value)
            );
        }
    }

    // Queries the virtual format as the original ASBD.
    // Required for accessing the original format for the 
    // player/recorder wrappers.
    internal CoreAudioTypes.AudioStreamBasicDescription VirtualFormatNative
    {
        get => GetAudioStreamBasicDescriptionValue(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioStreamProperties.kAudioStreamPropertyVirtualFormat
            )
        );
        set => SetAudioStreamBasicDescriptionValue(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioStreamProperties.kAudioStreamPropertyVirtualFormat
            ),
            value
        );
    }

    /// <summary>
    /// Provides the way for creating an event handle when the contents 
    /// of the <see cref="VirtualFormat"/> property do change.
    /// </summary>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to changes of the <see cref="VirtualFormat"/> property. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioStream" /> instance.
    /// </returns>
    public PropertyListenerHandle ConstructVirtualFormatChangedEvent() => new(
        this,
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioStreamProperties.kAudioStreamPropertyVirtualFormat
        )
    );

    /// <summary>
    /// An array of <see cref="WaveFormat"/> that describe the available data
    /// formats for the <see cref="AudioStream"/>. The virtual format refers to the data format in
    /// which all IOProcs for the owning <see cref="AudioDevice"/> will perform IO transactions.
    /// </summary>
    public RangedWaveFormat[] VirtualFormats
    {
        get
        {
            var asbd = GetArrayOfTPropertyValue<AudioStreamRangedDescription>(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioStreamProperties.kAudioStreamPropertyAvailableVirtualFormats
                )
            );
            RangedWaveFormat[] translated = new RangedWaveFormat[asbd.Length];
            for (int I = 0; I < asbd.Length; I++)
            {
                translated[I] = new RangedWaveFormat(asbd[I]);
            }
            return translated;
        }
    }

    /// <summary>
    /// A <see cref="WaveFormat"/> that describes the current data format for
    /// the <see cref="AudioStream"/>. The physical format refers to the data format in which the
    /// hardware for the owning <see cref="AudioDevice"/> performs its IO transactions.
    /// </summary>
    public WaveFormat PhysicalFormat => MacUtils.ConstructWaveFormatFromASBD(GetAudioStreamBasicDescriptionValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioStreamProperties.kAudioStreamPropertyPhysicalFormat
        )
    ));

    /// <summary>
    /// An array of <see cref="WaveFormat"/> that describe the available data
    /// formats for the <see cref="AudioStream"/>. The physical format refers to the data format
    /// in which the hardware for the owning <see cref="AudioDevice"/> performs its IO
    /// transactions.
    /// </summary>
    public RangedWaveFormat[] PhysicalFormats
    {
        get
        {
            var asbd = GetArrayOfTPropertyValue<AudioStreamRangedDescription>(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioStreamProperties.kAudioStreamPropertyAvailablePhysicalFormats
                )
            );
            RangedWaveFormat[] translated = new RangedWaveFormat[asbd.Length];
            for (int I = 0; I < asbd.Length; I++)
            {
                translated[I] = new RangedWaveFormat(asbd[I]);
            }
            return translated;
        }
    }
}