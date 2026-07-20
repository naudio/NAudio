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
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// The <see cref="AudioDevice"/> class is a subclass of the <see cref="AudioObject"/>.
/// The class has four scopes, <see cref="AudioObjectPropertyScopeConstants.Global"/>, 
/// <see cref="AudioObjectPropertyScopeConstants.Input"/>,
/// <see cref="AudioObjectPropertyScopeConstants.Output"/>, and <see cref="AudioObjectPropertyScopeConstants.PlayThrough"/>. 
/// The class has a main element and an element for each channel in each stream
/// numbered according to the starting channel number of each stream.
/// </summary>
public class AudioDevice : AudioObject
{
    internal AudioDevice(AudioObjectID objectID) : base(objectID) { }

    /// <summary>
    /// A <see cref="string"/> that contains the bundle ID for an application that provides a GUI for configuring the <see cref="AudioDevice"/>. <br />
    /// By default, the value of this property is the bundle ID for Audio MIDI Setup. 
    /// </summary>
    public string ConfigurationApplication => GetStringPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyConfigurationApplication));

    /// <summary>
    /// A <see cref="string"/> that contains a persistent identifier for the <see cref="AudioDevice"/>. <br />
    /// An <see cref="AudioDevice"/>'s UID is persistent across boots. 
    /// The content of the UID string is a black box and may contain information
    /// that is unique to a particular instance of an <see cref="AudioDevice"/>'s hardware or unique to the CPU.
    /// Therefore they are not suitable for passing between CPUs or for identifying similar models
    /// of hardware.
    /// </summary>
    public string DeviceUID => GetStringPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyDeviceUID));

    /// <summary>
    /// A <see cref="string"/> that contains a persistent identifier for the model of an <see cref="AudioDevice"/>. 
    /// The identifier is unique such that the identifier from two
    /// <see cref="AudioDevice"/>s are equal if and only if the two <see cref="AudioDevice"/>s are the exact same model from the same manufacturer. 
    /// Further, the identifier has to be the same no matter on what machine the <see cref="AudioDevice"/> appears. 
    /// </summary>
    public string ModelUID => GetStringPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyModelUID));

    /// <summary>
    /// A <see cref="TransportType"/> whose value indicates how the AudioDevice is connected to the CPU. <br />
    /// Constants for some of the values for this property can be found in the enum <see cref="TransportTypeConstants"/>.
    /// </summary>
    /// <param name="scope">The <see cref="AudioObjectPropertyScope">Property Scope</see> to query the transport type for</param>
    /// <returns>A <see cref="TransportType"/> value.</returns>
    public TransportType GetTransportType(AudioObjectPropertyScope scope)
        => (TransportType)GetUIntPropertyValue(
            new AudioObjectPropertyAddress(
                AudioDeviceProperties.kAudioDevicePropertyTransportType,
                scope,
                AudioObjectPropertyElement.Main
            )
        );

    /// <summary>
    /// An array of <see cref="AudioDevice"/>s for devices related to the <see cref="AudioDevice"/>. <br />
    /// For IOAudio-based devices, <see cref="AudioDevice"/>s are related if they share the same IOAudioDevice object.
    /// </summary>
    public AudioDevice[] RelatedDevices => GetAudioObjectValues(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyRelatedDevices),
        AudioObjectsConstructors.ConstructDevice
    );

    /// <summary>
    /// A UInt32 whose value indicates the clock domain to which this AudioDevice belongs. 
    /// AudioDevices that have the same value for this property are able to be synchronized in hardware.
    /// However, a value of 0 indicates that the clock domain for the device is unspecified and should 
    /// be assumed to be separate from every other device's clock domain, even if they have the value 
    /// of 0 as their clock domain as well.
    /// </summary>
    public uint ClockDomain => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyClockDomain));

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="true"/> means the device is ready and available and 
    /// <see langword="false"/> means the device is unusable and will most likely go away shortly.
    /// </summary>
    public bool IsAlive => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyDeviceIsAlive)) == 1U;

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="false"/> means the <see cref="AudioDevice"/> is not performing IO and
    /// a value of <see langword="true"/> means that it is. Note that the device can be running even if
    /// there are no active IOProcs such as by calling AudioDeviceStart() and
    /// passing a NULL IOProc. Note that the notification for this property is
    /// usually sent from the AudioDevice's IO thread.
    /// </summary>
    public bool IsRunning => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyDeviceIsRunning)) == 1U;

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that the <see cref="AudioDevice"/> is a possible selection for
    /// kAudioHardwarePropertyDefaultInputDevice or
    /// kAudioHardwarePropertyDefaultOutputDevice depending on the scope.
    /// </summary>
    /// <param name="scope">The scope to define the search for. Can only be <see cref="AudioObjectPropertyScopeConstants.Input"/> or <see cref="AudioObjectPropertyScopeConstants.Output"/>.</param>
    /// <returns>A value whether the current <see cref="AudioDevice"/> is a possible selection for an input/output device, depending on the used constant.</returns>
    public bool GetCanBeDefaultDevice(AudioObjectPropertyScope scope)
    {
        if (scope != AudioObjectPropertyScopeConstants.Input &&
            scope != AudioObjectPropertyScopeConstants.Output)
        {
            throw new ArgumentException("The scope can only be Input or Output", "scope");
        }
        else
        {
            return GetUIntPropertyValue(
                AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertyDeviceCanBeDefaultDevice,
                    scope
                )
            ) == 1U;
        }
    }

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that the <see cref="AudioDevice"/> is a possible selection for
    /// kAudioHardwarePropertyDefaultSystemOutputDevice.
    /// </summary>
    public bool CanBeDefaultSystemDevice => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyDeviceCanBeDefaultSystemDevice)) == 1U;

    /// <summary>
    /// A <see cref="uint"/> containing the number of frames of latency in the AudioDevice. Note
    /// that input and output latency may differ. Further, the <see cref="AudioDevice"/>'s
    /// AudioStreams may have additional latency so they should be queried as well.
    /// If both the device and the stream say they have latency, then the total
    /// latency for the stream is the device latency summed with the stream latency.
    /// </summary>
    public uint GetDeviceLatency(AudioObjectPropertyScope scope) => GetUIntPropertyValue(
        AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
            AudioDeviceProperties.kAudioDevicePropertyLatency,
            scope
        )
    );

    /// <summary>
    /// An array of <see cref="AudioStream"/>s that represent the AudioStreams of the <see cref="AudioDevice"/>. 
    /// Note that if a notification is received for this property, any cached AudioStreamIDs 
    /// for the device become invalid and need to be re-fetched.
    /// </summary>
    public AudioStream[] GetStreams(AudioObjectPropertyScope scope) => GetAudioObjectValues(
        AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
            AudioDeviceProperties.kAudioDevicePropertyStreams,
            scope
        ),
        AudioObjectsConstructors.ConstructStream
    );

    /// <summary>
    /// An array of <see cref="AudioControl"/> that represent the audio controls of the <see cref="AudioDevice"/>.
    /// Note that if a notification is received for this property, any cached 
    /// <see cref="AudioControl"/>s for the device become invalid and need to be re-fetched.
    /// </summary>
    public AudioControl[] ControlList => GetAudioObjectValues(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioDeviceProperties.kAudioObjectPropertyControlList
        ),
        AudioObjectsConstructors.ConstructAudioControl
    );

    /// <summary>
    /// A <see cref="uint"/> whose value indicates the number for frames in ahead (for output) 
    /// or behind (for input the current hardware position that is safe to do IO.
    /// </summary>
    /// <param name="scope">The direction to use (input/output)</param>
    /// <returns>The safety offset, as described above.</returns>
    /// <exception cref="ArgumentException"><paramref name="scope"/> not <see cref="AudioObjectPropertyScopeConstants.Input"/> or <see cref="AudioObjectPropertyScopeConstants.Output"/>.</exception>
    public uint GetSafetyOffset(AudioObjectPropertyScope scope)
    {
        if (scope != AudioObjectPropertyScopeConstants.Input &&
            scope != AudioObjectPropertyScopeConstants.Output)
        {
            throw new ArgumentException("The scope can only be Input or Output", "scope");
        }
        else
        {
            return GetUIntPropertyValue(
                AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertySafetyOffset,
                    scope
                )
            );
        }
    }

    /// <summary>
    /// A <see cref="double"/> that indicates the current nominal sample rate of the <see cref="AudioDevice"/>.
    /// </summary>
    public double NominalSampleRate => GetDoublePropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyNominalSampleRate));

    /// <summary>
    /// An array of pairs that indicates the valid ranges for the nominal sample rate of the <see cref="AudioClockDevice"/>.
    /// </summary>
    public (double min, double max)[] AvailableNomimalSampleRates
    {
        get
        {
            var t = GetAudioValueRangesPropertyValue(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertyAvailableNominalSampleRates
                )
            );
            var result = new (double min, double max)[t.Length];
            for (int I = 0; I < result.Length; I++)
            {
                var avr = t[I];
                result[I] = new(avr.mMinimum, avr.mMaximum);
            }
            return result;
        }
    }

    /// <summary>
    /// A <see cref="Uri"/> that indicates an image file that can be used to represent the device visually. 
    /// </summary>
    public Uri Icon => GetUriObjectValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyIcon));

    /// <summary>
    /// A UInt32 where a non-zero value indicates that the device is not included 
    /// in the normal list of devices provided by 
    /// <see cref="AudioSystemObject.Devices"/> nor can it be the default device. <br /> 
    /// Hidden devices can only be discovered by knowing their UID and 
    /// using <see cref="AudioSystemObject.ConvertUIDToDevice(string)"/>.
    /// </summary>
    public bool IsHidden => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyIsHidden)) == 1U;

    /// <summary>
    /// An array of two <see cref="uint"/>, the first for the left channel, the second for the
    /// right channel, that indicate the channel numbers to use for stereo IO on the
    /// device. The value of this property can be different for input and output and
    /// there are no restrictions on the channel numbers that can be used.
    /// </summary>
    public uint[] GetPreferredChannelsForStereo(AudioObjectPropertyScope scope)
    {
        if (scope != AudioObjectPropertyScopeConstants.Input &&
            scope != AudioObjectPropertyScopeConstants.Output)
        {
            throw new ArgumentException("The scope can only be Input or Output", "scope");
        }
        else
        {
            return GetArrayOfTPropertyValue<uint>(
                AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertyPreferredChannelsForStereo,
                    scope
                ),
                2
            );
        }
    }

    /// <summary>
    /// A <see cref="Speakers"/> value that indicates how each channel of the <see cref="AudioDevice"/> should be used.
    /// </summary>
    public Speakers GetPreferredChannelLayout(AudioObjectPropertyScope scope, out bool needs_resampling, out bool needs_extensible)
    {
        var acl = GetPreferredChannelLayout(scope);
        if (acl == IntPtr.Zero)
        {
            needs_resampling = false;
            needs_extensible = false;
            return Speakers.None;
        }
        else
        {
            return MacUtils.ConstructSpeakersValue(acl, out needs_resampling, out needs_extensible);
        }
    }

    internal IntPtr GetPreferredChannelLayout(AudioObjectPropertyScope scope)
    {
        if (scope != AudioObjectPropertyScopeConstants.Input &&
            scope != AudioObjectPropertyScopeConstants.Output)
        {
            throw new ArgumentException("The scope can only be Input or Output", "scope");
        }
        else
        {
            return GetArrayOfTPropertyValue<IntPtr>(
                AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertyPreferredChannelLayout,
                    scope
                ),
                1
            )[0];
        }
    }
}

