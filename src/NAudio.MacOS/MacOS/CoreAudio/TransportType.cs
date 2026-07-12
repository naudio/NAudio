/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Transport Type IDs <br />
/// Commonly used values for kAudioDevicePropertyTransportType 
/// and kAudioTransportManagerPropertyTransportType
/// </summary>
public enum TransportType : uint
{
    /// <summary>The transport type ID returned when a device doesn't provide a transport type.</summary>
    Unknown = 0,
}

/// <summary>
/// Provides common constants for the <see cref="TransportType"/> enumeration.
/// </summary>
public static class TransportTypeConstants
{
    /// <summary>The transport type ID for AudioDevices built into the system.</summary>
    public static readonly TransportType BuiltIn = (TransportType)MacUtils.ConstructUIntConstantValueFromString("bltn"u8);
    /// <summary>The transport type ID for aggregate devices.</summary>
    public static readonly TransportType Aggregate = (TransportType)MacUtils.ConstructUIntConstantValueFromString("grup"u8);
    /// <summary>The transport type ID for AudioDevices that don't correspond to real audio hardware.</summary>
    public static readonly TransportType Virtual = (TransportType)MacUtils.ConstructUIntConstantValueFromString("virt"u8);
    /// <summary>The transport type ID for AudioDevices connected via the PCI bus.</summary>
    public static readonly TransportType PCI = (TransportType)MacUtils.ConstructUIntConstantValueFromString("pci "u8);
    /// <summary>The transport type ID for AudioDevices connected via USB.</summary>
    public static readonly TransportType USB = (TransportType)MacUtils.ConstructUIntConstantValueFromString("usb "u8);
    /// <summary>The transport type ID for AudioDevices connected via FireWire.</summary>
    public static readonly TransportType FireWire = (TransportType)MacUtils.ConstructUIntConstantValueFromString("1394"u8);
    /// <summary>The transport type ID for AudioDevices connected via Bluetooth.</summary>
    public static readonly TransportType Bluetooth = (TransportType)MacUtils.ConstructUIntConstantValueFromString("blue"u8);
    /// <summary>The transport type ID for AudioDevices connected via Bluetooth Low Energy.</summary>
    public static readonly TransportType BluetoothLE = (TransportType)MacUtils.ConstructUIntConstantValueFromString("blea"u8);
    /// <summary>The transport type ID for AudioDevices connected via HDMI.</summary>
    public static readonly TransportType HDMI = (TransportType)MacUtils.ConstructUIntConstantValueFromString("hdmi"u8);
    /// <summary>The transport type ID for AudioDevices connected via DisplayPort.</summary>
    public static readonly TransportType DisplayPort = (TransportType)MacUtils.ConstructUIntConstantValueFromString("dprt"u8);
    /// <summary>The transport type ID for AudioDevices connected via AirPlay.</summary>
    public static readonly TransportType AirPlay = (TransportType)MacUtils.ConstructUIntConstantValueFromString("airp"u8);
    /// <summary>The transport type ID for AudioDevices connected via AVB.</summary>
    public static readonly TransportType AVB = (TransportType)MacUtils.ConstructUIntConstantValueFromString("eavb"u8);
    /// <summary>The transport type ID for AudioDevices connected via Thunderbolt.</summary>
    public static readonly TransportType Thunderbolt = (TransportType)MacUtils.ConstructUIntConstantValueFromString("thun"u8);
    /// <summary>The transport type ID for Continuity Capture AudioDevices connected via a cable.</summary>
    public static readonly TransportType ContinuityCaptureWired = (TransportType)MacUtils.ConstructUIntConstantValueFromString("ccwd"u8);
    /// <summary>The transport type ID for Continuity Capture AudioDevices connected via wireless networking.</summary>
    public static readonly TransportType ContinuityCaptureWireless = (TransportType)MacUtils.ConstructUIntConstantValueFromString("ccwl"u8);
}