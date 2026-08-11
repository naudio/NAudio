// This interop definition was derived from the file AudioHardware.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// The audio clock device represents the clock of an <see cref="AudioDevice"/> object.
/// </summary>
public sealed class AudioClockDevice : AudioObject
{
    internal AudioClockDevice(AudioObjectID id) : base(id) { }

    /// <summary>
    /// A <see cref="string"/> that contains a persistent identifier for the <see cref="AudioClockDevice"/>.
    /// An <see cref="AudioClockDevice"/>'s UID is persistent across boots. 
    /// The content of the UID string is a black box and may contain information 
    /// that is unique to a particular instance of an clock's hardware or unique to the CPU. 
    /// Therefore they are not suitable for passing between CPUs or for identifying similar
    /// models of hardware. 
    /// </summary>
    public string DeviceUID => GetStringPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyDeviceUID));

    /// <summary>
    /// A <see cref="CoreAudio.TransportType"/> whose value indicates how the AudioClockDevice is connected to the CPU.
    /// Constants for some of the values for this property can be found in the class <see cref="TransportTypeConstants"/>.
    /// </summary>
    public TransportType TransportType => (TransportType)GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyTransportType));

    /// <summary>
    /// A <see cref="uint"/> whose value indicates the clock domain to which this <see cref="AudioClockDevice"/> belongs.
    /// AudioClockDevices and AudioDevices that have the same value for this property are able 
    /// to be synchronized in hardware.
    /// However, a value of 0 indicates that the clock domain for the device is
    /// unspecified and should be assumed to be separate from every other device's
    /// clock domain, even if they have the value of 0 as their clock domain as well.
    /// </summary>
    public uint ClockDomain => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyClockDomain));

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="true"/> means 
    /// the device is ready and available and <see langword="false"/>
    /// means the device is usable and will most likely go away shortly.
    /// </summary>
    public bool IsAlive => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyDeviceIsAlive)) == 1U;

    /// <summary>
    /// A <see cref="bool"/> where a value of <see langword="false"/>
    /// means the <see cref="AudioClockDevice"/> is not providing
    /// times and a value of <see langword="true"/> means that it is. <br />
    /// Note that the notification for this property is usually sent 
    /// from the <see cref="AudioClockDevice"/>'s IO thread.
    /// </summary>
    public bool IsRunning => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyDeviceIsRunning)) == 1U;

    /// <summary>
    /// A <see cref="uint"/> containing the number of 
    /// frames of latency in the <see cref="AudioClockDevice"/>.
    /// </summary>
    public uint Latency => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyLatency));

    /// <summary>
    /// A <see cref="double"/> that indicates the current nominal sample rate of the <see cref="AudioClockDevice"/>.
    /// </summary>
    public double NominalSampleRate => GetDoublePropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioClockDeviceProperties.kAudioClockDevicePropertyNominalSampleRate));

    /// <summary>
    /// An array of pairs that indicates the valid ranges for the nominal sample rate of the <see cref="AudioClockDevice"/>.
    /// </summary>
    public (double min, double max)[] AvailableNomimalSampleRates
    {
        get
        {
            var t = GetAudioValueRangesPropertyValue(
                AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                    AudioClockDeviceProperties.kAudioClockDevicePropertyAvailableNominalSampleRates
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
}