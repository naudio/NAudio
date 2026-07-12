/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using System;
using NAudio.Wave;
using NAudio.Utils;
using NAudio.MacOS.CoreAudioTypes;
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
    /// A <see cref="bool"/> where a value of <see langword="false"/> means that this AudioStream is an 
    /// output stream and a value of <see langword="true"/> means that it is an input stream.
    /// </summary>
    // mdcdi1315: Maybe put an enumeration with two single cases (Input, Output) for it?
    public bool Direction => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyDirection)) == 1U;

    /// <summary>
    /// A <see cref="uint"/> that specifies the first element in the owning 
    /// device that corresponds to element one of this stream.
    /// </summary>
    public uint StartingChannel => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioStreamProperties.kAudioStreamPropertyStartingChannel));

    /// <summary>
    /// A <see cref="AudioStreamTerminalType"/> whose value describes the general kind of functionality attached to the AudioStream.
    /// </summary>
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
    /// IOProcs for the owning <see cref="AudioDevice"/> will perform IO transactions.
    /// </summary>
    public WaveFormat VirtualFormat
    {
        get
        {
            var asbd = GetArrayOfTPropertyValue<AudioStreamBasicDescription>(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioStreamProperties.kAudioStreamPropertyVirtualFormat
                ), 1
            );
            return MacUtils.ConstructWaveFormatFromASBD(asbd[0]);
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetArrayOfTPropertyValue(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioStreamProperties.kAudioStreamPropertyVirtualFormat
                ), [MacUtils.ConstructASBDFromWaveFormat(value)]
            );
        }
    }

    /// <summary>
    /// An array of <see cref="WaveFormat"/> that describe the available data
    /// formats for the <see cref="AudioStream"/>. The virtual format refers to the data format in
    /// which all IOProcs for the owning <see cref="AudioDevice"/> will perform IO transactions.
    /// </summary>
    public unsafe RangedWaveFormat[] VirtualFormats
    {
        get
        {
            var address = AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioStreamProperties.kAudioStreamPropertyAvailableVirtualFormats
            );
            var asbd = GetArrayOfTPropertyValue<AudioStreamRangedDescription>(address, (int)(QueryPropertySize(address) / sizeof(AudioStreamRangedDescription)));
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
    public WaveFormat PhysicalFormat
    {
        get
        {
            var asbd = GetArrayOfTPropertyValue<AudioStreamBasicDescription>(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioStreamProperties.kAudioStreamPropertyPhysicalFormat
                ), 1
            );
            return MacUtils.ConstructWaveFormatFromASBD(asbd[0]);
        }
    }

    /// <summary>
    /// An array of <see cref="WaveFormat"/> that describe the available data
    /// formats for the <see cref="AudioStream"/>. The physical format refers to the data format
    /// in which the hardware for the owning <see cref="AudioDevice"/> performs its IO
    /// transactions.
    /// </summary>
    public unsafe RangedWaveFormat[] PhysicalFormats
    {
        get
        {
            var address = AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioStreamProperties.kAudioStreamPropertyAvailablePhysicalFormats
            );
            var asbd = GetArrayOfTPropertyValue<AudioStreamRangedDescription>(address, (int)(QueryPropertySize(address) / sizeof(AudioStreamRangedDescription)));
            RangedWaveFormat[] translated = new RangedWaveFormat[asbd.Length];
            for (int I = 0; I < asbd.Length; I++)
            {
                translated[I] = new RangedWaveFormat(asbd[I]);
            }
            return translated;
        }
    }
}