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
    /// Provides the way for creating an event handle when the value 
    /// of the <see cref="IsAlive"/> property do change.
    /// </summary>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to changes of the <see cref="IsAlive"/> property. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioDevice" /> instance.
    /// </returns>
    public PropertyListenerHandle ConstructIsAliveChangedEvent() => new(
        this,
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioDeviceProperties.kAudioDevicePropertyDeviceIsAlive
        )
    );

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
    /// Provides the way for creating an event handle when the contents 
    /// of the <see cref="GetStreams"/> method do change.
    /// </summary>
    /// <param name="scope">The <see cref="AudioObjectPropertyScope"/> of the device's scope to query</param>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to changes of the <see cref="GetStreams"/> method. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioDevice" /> instance.
    /// </returns>
    public PropertyListenerHandle ConstructStreamsChangedEvent(AudioObjectPropertyScope scope) => new(
        this,
        AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
            AudioDeviceProperties.kAudioDevicePropertyStreams,
            scope
        )
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
    /// Provides the way for creating an event handle when the contents 
    /// of the <see cref="ControlList"/> property do change.
    /// </summary>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to changes of the <see cref="ControlList"/> property. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioDevice" /> instance.
    /// </returns>
    public PropertyListenerHandle ConstructControlListChangedEvent() => new(
        this,
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioDeviceProperties.kAudioObjectPropertyControlList
        )
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

    internal unsafe IntPtr GetPreferredChannelLayout(AudioObjectPropertyScope scope)
    {
        if (scope != AudioObjectPropertyScopeConstants.Input &&
            scope != AudioObjectPropertyScopeConstants.Output)
        {
            throw new ArgumentException("The scope can only be Input or Output", "scope");
        }
        else
        {
            IntPtr ret;
            uint size = (uint)IntPtr.Size;
            GetPropertyValueCustom(
                AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertyPreferredChannelLayout,
                    scope
                ),
                ref size,
                new(&ret)
            );
            return ret;
        }
    }

    /// <summary>
    /// Provides the way for creating an event handle when the audio
    /// device has been changed.
    /// </summary>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to an event that is fired when the device configuration has been altered. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioDevice" /> instance.
    /// </returns>
    public PropertyListenerHandle GetDeviceHasChangedEvent() => new(
        this,
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioDeviceProperties.kAudioDevicePropertyDeviceHasChanged
        )
    );

    /// <summary>
    /// A <see cref="bool"/> where <see langword="true"/> means that the AudioDevice is running in at least one
    /// process on the system and <see langword="false"/> means that it isn't running at all.
    /// </summary>
    public bool IsRunningSomewhere => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyDeviceIsRunningSomewhere)) != 0U;

    /// <summary>
    /// Provides the way for creating an event handle when an I/O 
    /// procedure in this audio device has been run past it's deadline.
    /// </summary>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to an event that is fired when the <see cref="AudioDevice"/> is having a <see cref="CoreAudioIOProcedure"/> that has run past it's deadline. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioDevice" /> instance.
    /// </returns>
    public PropertyListenerHandle ConstructProcessorOverloadedEvent() => new(
        this,
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioDeviceProperties.kAudioDeviceProcessorOverload
        )
    );

    /// <summary>
    /// Provides the way for creating an event handle when I/O has been abnormally stopped.
    /// </summary>
    /// <returns>
    /// A new <see cref="PropertyListenerHandle"/> that can be used to
    /// listen to an event that is fired when the <see cref="AudioDevice"/> has it's I/O abnormally stopped. <br />
    /// It's <see cref="PropertyListenerHandle.OriginatingObject"/> property value is this <see cref="AudioDevice" /> instance.
    /// </returns>
    public PropertyListenerHandle ConstructIOStoppedAbnormallyEvent() => new(
        this,
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioDeviceProperties.kAudioDevicePropertyIOStoppedAbnormally
        )
    );

    /// <summary>
    /// A <see cref="bool"/> indicating the process that currently owns exclusive access to the
    /// AudioDevice or a value of <see langword="false"/> indicating that the device is currently
    /// available to all processes. If the <see cref="AudioDevice"/> is in a non-mixable mode,
    /// the HAL will automatically take hog mode on behalf of the first process to
    /// start an <see cref="CoreAudioIOProcedure"/>. <br /> <br />
    /// Note that when setting this property, the value passed in is ignored. If
    /// another process owns exclusive access, that remains unchanged. If the
    /// current process owns exclusive access, it is released and made available to
    /// all processes again. If no process has exclusive access (meaning the current
    /// value is -1), this process gains ownership of exclusive access.
    /// </summary>
    public bool HogMode
    {
        get => unchecked((int)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyHogMode))) != -1;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyHogMode), 0U);
    }

    /// <summary>
    /// A <see cref="uint"/> whose value indicates the number of frames in the IO buffers.
    /// </summary>
    public uint BufferFrameSize => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyBufferFrameSize));

    /// <summary>
    /// A pair indicating the minimum and maximum values, inclusive, for <see cref="BufferFrameSize"/>.
    /// </summary>
    public (double min, double max) BufferFrameSizeRange
    {
        get
        {
            var avr = GetAudioValueRangePropertyValue(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioDeviceProperties.kAudioDevicePropertyBufferFrameSizeRange
                )
            );
            return (avr.mMinimum, avr.mMaximum);
        }
    }

    /// <summary>
    /// A <see cref="uint"/> that, if implemented by a device, indicates that the sizes of the
    /// buffers passed to an IOProc will vary by a small amount. The value of this
    /// property will indicate the largest buffer that will be passed and
    /// <see cref="BufferFrameSize"/> will indicate the smallest buffer that
    /// will get passed to the IOProc. The usage of this property is narrowed to
    /// only allow for devices whose buffer sizes vary by small amounts greater than
    /// <see cref="BufferFrameSize"/>. It is not intended to be a license for
    /// devices to be able to send buffers however they please. Rather, it is
    /// intended to allow for hardware whose natural rhythms lead to this necessity.
    /// </summary>
    public uint UsesVariableBufferFrameSizes => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyUsesVariableBufferFrameSizes));

    /// <summary>
    /// A <see cref="float"/> whose range is from 0 to 1. This value indicates how much of the
    /// client portion of the IO cycle the process will use. The client portion of
    /// the IO cycle is the portion of the cycle in which the device calls the
    /// IOProcs so this property does not the apply to the duration of the entire
    /// cycle.
    /// </summary>
    public float IOCycleUsage
    {
        get => GetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyIOCycleUsage));
        set => SetFloatPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyIOCycleUsage), value);
    }

    /// <summary>
    /// A <see cref="bool"/> array which details the stream usage of a given I/O procedure. 
    /// If a stream is marked as not being used (which the value of each array element will be <see langword="false"/>), the given <see cref="CoreAudioIOProcedure"/> 
    /// will see a corresponding NULL buffer pointer in the AudioBufferList
    /// passed to its IO proc. Note that the number of streams detailed in the
    /// <see cref="bool"/> array must include all the streams of that
    /// direction on the device.
    /// </summary>
    /// <param name="procedure">The Core Audio I/O procedure to query the usage for.</param>
    /// <param name="scope">The scope that the stream usage is to be retrieved for.</param>
    /// <returns>A <see cref="bool"/> array that indicates which streams are enabled and which are not. Use the <see cref="GetStreams"/> method to map each index to an <see cref="AudioStream"/> object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="procedure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="procedure"/> is invalid.</exception>
    public unsafe bool[] GetStreamUsage(CoreAudioIOProcedure procedure, AudioObjectPropertyScope scope)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        if (procedure.IsClosed || procedure.IsInvalid)
        {
            throw new InvalidOperationException("The specified IO procedure has been disposed of.");
        }
        else
        {
            var address = AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                AudioDeviceProperties.kAudioDevicePropertyIOProcStreamUsage,
                scope
            );
            var size = QueryPropertySize(address);
            var memBlock = AudioHardwareIOProcStreamUsage.CreateStreamUsage((AudioDeviceIOProc)procedure.DangerousGetHandle(), size);
            try
            {
                GetPropertyValueCustom(address, ref size, memBlock);
                var cBuffers = AudioHardwareIOProcStreamUsage.GetNumberOfStreams(memBlock);
                var data = new bool[cBuffers];
                for (var I = 0U; I < cBuffers; I++)
                {
                    data[I] = AudioHardwareIOProcStreamUsage.GetStream(memBlock, I);
                }
                return data;
            }
            finally
            {
                AudioHardwareIOProcStreamUsage.DeleteStreamUsage(memBlock);
            }
        }
    }

    /// <summary>
    /// Set a <see cref="bool"/> array which details the stream usage of a given I/O procedure. 
    /// If a stream is marked as not being used (which the value of each array element will be <see langword="false"/>), the given <see cref="CoreAudioIOProcedure"/> 
    /// will see a corresponding NULL buffer pointer in the AudioBufferList
    /// passed to its IO proc. Note that the number of streams detailed in the
    /// <see cref="bool"/> array must include all the streams of that
    /// direction on the device.
    /// </summary>
    /// <param name="procedure">The Core Audio I/O procedure to assign the usage for.</param>
    /// <param name="scope">The scope that the stream usage is to be retrieved for.</param>
    /// <param name="streamsToEnableOrDisable">The streams to enable/disable.</param>
    /// <exception cref="ArgumentNullException"><paramref name="procedure"/> and/or <paramref name="streamsToEnableOrDisable"/> are <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="procedure"/> is invalid.</exception>
    public unsafe void SetStreamUsage(CoreAudioIOProcedure procedure, AudioObjectPropertyScope scope, bool[] streamsToEnableOrDisable)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(streamsToEnableOrDisable);
        if (procedure.IsClosed || procedure.IsInvalid)
        {
            throw new InvalidOperationException("The specified IO procedure has been disposed of.");
        }
        else
        {
            var address = AudioObjectPropertyAddress.CreateWithScopeAndMainElement(
                AudioDeviceProperties.kAudioDevicePropertyIOProcStreamUsage,
                scope
            );
            var memBlock = AudioHardwareIOProcStreamUsage.CreateStreamUsage((AudioDeviceIOProc)procedure.DangerousGetHandle(), (uint)streamsToEnableOrDisable.LongLength, out var size);
            for (uint I = 0; I < AudioHardwareIOProcStreamUsage.GetNumberOfStreams(memBlock); I++)
            {
                AudioHardwareIOProcStreamUsage.SetStream(memBlock, I, streamsToEnableOrDisable[I]);
            }
            try
            {
                SetPropertyCustom(address, size, memBlock);
            }
            finally
            {
                AudioHardwareIOProcStreamUsage.DeleteStreamUsage(memBlock);
            }
        }
    }

    /// <summary>
    /// A <see cref="double"/> that indicates the current actual sample rate of the AudioDevice
    /// as measured by its time stamps.
    /// </summary>
    public double ActualSampleRate => GetDoublePropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyActualSampleRate));

    /// <summary>
    /// A string that contains the UID for the AudioClockDevice that is currently
    /// serving as the main time base of the device.
    /// </summary>
    public string ClockDevice => GetStringPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyClockDevice));

    /// <summary>
    /// A <see cref="bool"/> where a <see langword="true"/> value indicates 
    /// that the current process's audio will be zeroed out by the system. 
    /// Note that this property does not apply to aggregate devices, 
    /// just real, physical devices.
    /// </summary>
    public bool ProcessMute
    {
        get => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyProcessMute)) != 0;
        set => SetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioDeviceProperties.kAudioDevicePropertyProcessMute), value ? 1U : 0U);
    }
}

