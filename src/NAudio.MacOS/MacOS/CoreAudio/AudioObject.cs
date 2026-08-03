// This interop definition was derived from the file AudioHardware.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System;
using System.Numerics;
using System.Diagnostics;

using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the base class for the Core Audio HAL audio objects. <br /> <br />
/// 
/// AudioObjects all have a set of properties that describe and manipulate their state. 
/// A property is accessed via an ordered triple. 
/// The first coordinate is the selector which describes the property. 
/// The other two ordinates are the scope and element that identify the particular part of the object in which to look for the selector. 
/// The AudioObjectPropertyAddress structure encapsulates the property address. 
/// The value of a property is an untyped block of data whose content depends on the specifics of the selector. 
/// Some selectors also require the use of a qualifier when querying. 
/// The qualifier allows for additional information to be provided to used in the manipulation of the property. 
/// Changing the value of a property is always considered asynchronous. <br /> <br />
/// 
/// Applications use the routines AudioObjectHasProperty(), AudioObjectIsPropertySettable() and
/// AudioObjectGetPropertyDataSize() to find useful meta-information about the property. Apps use
/// AudioObjectGetPropertyData() and AudioObjectSetPropertyData() to manipulate the value of the
/// property. Apps use AudioObjectAddPropertyListener() and AudioObjectRemovePropertyListener() to
/// register/unregister a function that is to be called when a given property's value changes. <br /> <br />
/// 
/// The class of an AudioObject determines the basic functionality of the object in terms of what
/// functions will operate on it as well as the set of properties that can be expected to be
/// implemented by the object. The set of available classes for objects is limited to those defined
/// here. There are no other classes. The set of classes is arranged in a hierarchy such that one
/// class inherits the properties/routines of its super class. <br /> <br />
/// 
/// The base class for all AudioObjects is the class AudioObject. As such, each AudioObject will 
/// provide basic properties such as its class, its human readable name, and the other
/// AudioObjects it contains. Other important classes include AudioSystemObject, AudioDevice, and
/// AudioStream. <br /> <br />
/// 
/// The AudioObjects in the HAL are arranged in a containment hierarchy. The root of the hierarchy
/// is the one and only instance of the AudioSystemObject class. The properties of the
/// AudioSystemObject describe the process global settings such as the various default devices and
/// the notification run loop. The AudioSystemObject also contains all the AudioDevices that are
/// available. <br /> <br />
/// 
/// Instances of the AudioDevice class encapsulate individual audio devices. An AudioDevice serves
/// as the basic unit of IO. It provides a single IO cycle, a timing source based on it, and all the
/// buffers synchronized to it. The IO cycle presents all the synchronized buffers to the client in
/// the same call out along with time stamps that specify the current time, when the input data was
/// acquired and when the output data will be presented. <br /> <br />
/// 
/// AudioDevices contain instances of the AudioStream class. An AudioStream represents a single
/// buffer of data for transferring across the user/kernel boundary. As such, AudioStreams are the
/// gatekeepers of format information. Each has its own format and list of available formats.
/// AudioStreams can provide data in any format, including encoded formats and non-audio formats. If
/// the format is a linear PCM format, the data will always be presented as 32 bit, native endian
/// floating point. All conversions to and from the true physical format of the hardware is handled
/// by the device's driver. <br /> <br />
/// 
/// Both AudioDevices and AudioStreams can contain instances of the AudioControl class or its many
/// subclasses. An AudioControl provides properties that describe/manipulate a particular aspect of
/// the object such as gain, mute, data source selection, etc. Many common controls are also
/// also available as properties on the AudioDevice or AudioStream.
/// </summary>
public abstract class AudioObject :
    IEquatable<AudioObject>,
    IEqualityOperators<AudioObject, AudioObject, bool>
{
    #region Private definitions

    internal readonly AudioObjectID objectId;

    internal AudioObject(AudioObjectID objectId)
    {
        if (AudioObjectID.IsUnknownID(objectId))
        {
            throw new InvalidOperationException("Cannot initialize a wrapper AudioObject from an invalid id. Use null reference instead.");
        }
        else
        {
            this.objectId = objectId;
        }
    }

    #region Interop definitions

    private protected PropertyListenerHandle CreateNotificationHandler(AudioObjectPropertyAddress address) => new(this, address);

    private protected unsafe string GetStringPropertyValue(AudioObjectPropertyAddress address)
    {
        IntPtr ptr;
        uint size = (uint)sizeof(nint);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&ptr))
        );
        using CoreFoundationApi.CFString cfs = new(ptr);
        return cfs.ToString();
    }

    private protected string GetStringPropertyValueOrEmptyIfNotExisting(AudioObjectPropertyAddress address)
    {
        try
        {
            return GetStringPropertyValue(address);
        }
        catch (CoreAudioPropertyNotFoundException)
        {
            return string.Empty;
        }
    }

    private protected unsafe string GetStringPropertyValue<TQ>(AudioObjectPropertyAddress address, Span<TQ> qualifierData)
        where TQ : unmanaged
    {
        IntPtr ptr;
        uint size = (uint)sizeof(nint);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, qualifierData, ref size, new(&ptr))
        );
        using CoreFoundationApi.CFString cfs = new(ptr);
        return cfs.ToString();
    }

    private protected unsafe uint GetUIntPropertyValue(AudioObjectPropertyAddress address)
    {
        uint returnValue;
        uint size = sizeof(uint);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&returnValue))
        );
        return returnValue;
    }

    private protected unsafe uint GetUIntPropertyValue<T>(AudioObjectPropertyAddress address, Span<T> qualifier)
        where T : unmanaged
    {
        uint returnValue;
        uint size = sizeof(uint);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, qualifier, ref size, new(&returnValue))
        );
        return returnValue;
    }

    private protected unsafe float GetFloatPropertyValue(AudioObjectPropertyAddress address)
    {
        float returnValue;
        uint size = sizeof(float);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&returnValue))
        );
        return returnValue;
    }

    private protected unsafe double GetDoublePropertyValue(AudioObjectPropertyAddress address)
    {
        double returnValue;
        uint size = sizeof(double);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&returnValue))
        );
        return returnValue;
    }

    private protected unsafe AudioValueRange GetAudioValueRangePropertyValue(AudioObjectPropertyAddress address)
    {
        AudioValueRange returnValue;
        uint size = (uint)sizeof(AudioValueRange);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&returnValue))
        );
        return returnValue;
    }

    private protected unsafe AudioValueRange[] GetAudioValueRangesPropertyValue(AudioObjectPropertyAddress address)
    {
        uint nativeSize = QueryPropertySize(address);
        var returnValue = new AudioValueRange[nativeSize / sizeof(AudioValueRange)];
        fixed (AudioValueRange* memBlock = returnValue)
        {
            ThrowOnStatusError(
                address,
                NativeMethods.AudioObjectGetPropertyData(objectId, address, ref nativeSize, new(memBlock))
            );
        }
        return returnValue;
    }

    private protected unsafe T[] GetArrayOfTPropertyValue<T>(AudioObjectPropertyAddress address, int size)
        where T : unmanaged
    {
        var returnValue = new T[size];
        uint nativeSize = (uint)(sizeof(T) * returnValue.LongLength);
        fixed (T* memBlock = returnValue)
        {
            ThrowOnStatusError(
                address,
                NativeMethods.AudioObjectGetPropertyData(objectId, address, ref nativeSize, new(memBlock))
            );
        }
        return returnValue;
    }

    private protected unsafe T[] GetArrayOfTPropertyValue<T>(AudioObjectPropertyAddress address)
        where T : unmanaged
    {
        uint nativeSize = QueryPropertySize(address);
        var returnValue = new T[nativeSize / sizeof(T)];
        fixed (T* memBlock = returnValue)
        {
            ThrowOnStatusError(
                address,
                NativeMethods.AudioObjectGetPropertyData(objectId, address, ref nativeSize, new(memBlock))
            );
        }
        return returnValue;
    }

    private protected unsafe T GetAudioObjectValue<T>(AudioObjectPropertyAddress address, Func<AudioObjectID, T> factory)
        where T : AudioObject
    {
        AudioObjectID id;
        uint size = (uint)sizeof(AudioObjectID);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&id))
        );
        return AudioObjectID.IsUnknownID(id) ? null : factory(id);
    }

    private protected unsafe T GetAudioObjectValue<T, TQualifier>(AudioObjectPropertyAddress address, Func<AudioObjectID, T> factory, Span<TQualifier> qualifier_data)
        where T : AudioObject
        where TQualifier : unmanaged
    {
        AudioObjectID id;
        uint size = (uint)sizeof(AudioObjectID);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, qualifier_data, ref size, new(&id))
        );
        return AudioObjectID.IsUnknownID(id) ? null : factory(id);
    }

    private protected unsafe T[] GetAudioObjectValues<T>(AudioObjectPropertyAddress address, Func<AudioObjectID, T> factory)
        where T : AudioObject
    {
        uint size = QueryPropertySize(address);
        AudioObjectID[] ids = new AudioObjectID[size / sizeof(AudioObjectID)];
        fixed (AudioObjectID* pids = ids)
        {
            ThrowOnStatusError(
                address,
                NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(pids))
            );
        }
        AudioObjectID id;
        // Make sure to recompute the buffer's size, an update could happen 
        // after querying the property's size and before getting 
        // the actual data.
        var values = new T[size / sizeof(AudioObjectID)];
        for (int I = 0; I < values.Length; I++)
        {
            id = ids[I];
            values[I] = AudioObjectID.IsUnknownID(id) ? null : factory(id);
        }
        return values;
    }

    private protected unsafe Uri GetUriObjectValue(AudioObjectPropertyAddress address)
    {
        IntPtr cfUrl;
        uint size = (uint)IntPtr.Size;
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, new(&cfUrl))
        );
        using var url = new CoreFoundationApi.CFURL(cfUrl);
        return url.ToUri();
    }

    private protected unsafe AudioStreamBasicDescription GetAudioStreamBasicDescriptionValue(AudioObjectPropertyAddress address)
    {
        AudioStreamBasicDescription retDesc;
        uint size = (uint)sizeof(AudioStreamBasicDescription);
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(
                objectId,
                address,
                ref size,
                new(&retDesc)
            )
        );
        return retDesc;
    }

    private protected unsafe void SetFloatPropertyValue(AudioObjectPropertyAddress address, float value)
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectSetPropertyData(objectId, address, sizeof(uint), new(&value))
        );
    }

    private protected unsafe void SetUIntPropertyValue(AudioObjectPropertyAddress address, uint value)
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectSetPropertyData(objectId, address, sizeof(uint), new(&value))
        );
    }

    private protected unsafe void SetArrayOfTPropertyValue<T>(AudioObjectPropertyAddress address, T[] value)
        where T : unmanaged
    {
        fixed (T* memBlock = value)
        {
            ThrowOnStatusError(
                address,
                NativeMethods.AudioObjectSetPropertyData(objectId, address, (uint)(sizeof(T) * value.LongLength), new(memBlock))
            );
        }
    }

    private protected unsafe void SetAudioStreamBasicDescriptionValue(AudioObjectPropertyAddress address, AudioStreamBasicDescription asbd)
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectSetPropertyData(objectId, address, (uint)sizeof(AudioStreamBasicDescription), new(&asbd))
        );
    }

    private protected void GetPropertyValueCustom(AudioObjectPropertyAddress address, ref uint size, IntPtr value)
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyData(objectId, address, ref size, value)
        );
    }

    private protected void SetPropertyCustom(AudioObjectPropertyAddress address, uint size, IntPtr value)
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectSetPropertyData(objectId, address, size, value)
        );
    }

    /// <summary>
    /// Queries the number of total bytes the given property requires for it's output buffer.
    /// </summary>
    private protected uint QueryPropertySize(AudioObjectPropertyAddress address)
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyDataSize(objectId, address, out var size)
        );
        return size;
    }

    /// <summary>
    /// Queries the number of total bytes the given property requires for it's output buffer.
    /// </summary>
    private protected uint QueryPropertySize<T>(AudioObjectPropertyAddress address, Span<T> qualifierData)
        where T : unmanaged
    {
        ThrowOnStatusError(
            address,
            NativeMethods.AudioObjectGetPropertyDataSize(objectId, address, qualifierData, out var size)
        );
        return size;
    }

    #endregion

    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static void ThrowOnStatusError(AudioObjectPropertyAddress address, int status)
    {
        if (status == Interop.ErrorConstants.kAudioHardwareUnknownPropertyError)
        {
            throw new CoreAudioPropertyNotFoundException((uint)address.mSelector);
        }
        else
        {
            CoreAudioException.ThrowIfError(status);
        }
    }

    #endregion

    /// <summary>Gets the actual ID of this class, as reported by the native code.</summary>
    public uint ClassID => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyClass));

    /// <summary>Gets the actual ID of the base class of this class, as reported by the native code.</summary>
    public uint BaseClassID => GetUIntPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyBaseClass));

    /// <summary>Gets the name of this audio object.</summary>
    public string Name => GetStringPropertyValueOrEmptyIfNotExisting(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyName));

    /// <summary>Gets the manufacturer's model name of this audio object.</summary>
    public string ModelName => GetStringPropertyValueOrEmptyIfNotExisting(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyModelName));

    /// <summary>Gets the manufacturer of this audio object.</summary>
    public string Manufacturer => GetStringPropertyValueOrEmptyIfNotExisting(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyManufacturer));

    /// <summary>Gets the manufacturer's serial number of this audio object.</summary>
    /// <remarks>This is purely an informational value.</remarks>
    public string SerialNumber => GetStringPropertyValueOrEmptyIfNotExisting(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertySerialNumber));

    /// <summary>Gets the manufacturer's firmware version of this audio object.</summary>
    /// <remarks>This is purely an informational value.</remarks>
    public string FirmwareVersion => GetStringPropertyValueOrEmptyIfNotExisting(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyFirmwareVersion));

    /// <summary>
    /// An <see cref="AudioObject"/> that identifies the <see cref="AudioObject"/> that owns this <see cref="AudioObject"/>. 
    /// Note that all AudioObjects are owned by some other <see cref="AudioObject"/>. <br />
    /// The only exception is the <see cref="AudioSystemObject"/>, for which the value of this property is <see langword="null"/>.
    /// </summary>
    public AudioObject Owner => GetAudioObjectValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyOwner), AudioObjectsConstructors.ConstructAudioObject);

    /// <summary>
    /// An array of <see cref="AudioObject"/>s that represent all the AudioObjects owned by this object. 
    /// </summary>
    public AudioObject[] OwnedObjects => GetAudioObjectValues(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(AudioObjectProperties.kAudioObjectPropertyOwnedObjects), AudioObjectsConstructors.ConstructAudioObject);

    /// <summary>
    /// Determines whether two <see cref="AudioObject"/> instances are pointing to the same Core Audio object.
    /// </summary>
    /// <remarks>
    /// Note that this operator tests whether the wrapped interop objects are the same;
    /// as such, you could have two <see cref="AudioObject"/> instances that are different in .NET
    /// (the test <see cref="object.ReferenceEquals(object?, object?)"/> is <see langword="false"/>)
    /// but both are targeting the same natively wrapped object. This is actually valid,
    /// because these classes are just wrapping the objects, and as such they have 
    /// the exact same effects even if two different wrapper references are created.
    /// </remarks>
    /// <param name="object1">The first object.</param>
    /// <param name="object2">The second object.</param>
    /// <returns>A value whether the two objects are both non-null and are pointing to the same Core Audio object.</returns>
    public static bool operator ==(AudioObject object1, AudioObject object2)
    {
        bool object2IsNull = object2 is null;
        return object1 is null ? object2IsNull : !object2IsNull && object1.objectId == object2.objectId;
    }

    /// <summary>
    /// Determines whether two <see cref="AudioObject"/> instances are pointing to a different Core Audio object.
    /// </summary>
    /// <remarks>
    /// Note that this operator tests whether the wrapped interop objects are the same;
    /// as such, you could have two <see cref="AudioObject"/> instances that are different in .NET
    /// (the test <see cref="object.ReferenceEquals(object?, object?)"/> is <see langword="false"/>)
    /// but both are targeting the same natively wrapped object. This is actually valid,
    /// because these classes are just wrapping the objects, and as such they have 
    /// the exact same effects even if two different wrapper references are created.
    /// </remarks>
    /// <param name="object1">The first object.</param>
    /// <param name="object2">The second object.</param>
    /// <returns>A value whether the two objects are both non-null and are pointing to a different Core Audio object.</returns>
    public static bool operator !=(AudioObject object1, AudioObject object2)
    {
        bool object2IsNull = object2 is null;
        return object1 is null ? !object2IsNull : object2IsNull || object1.objectId != object2.objectId;
    }

    /// <summary>Retrieves a hash code for this Core Audio object instance.</summary>
    /// <returns>A hash code.</returns>
    public override int GetHashCode() => objectId.GetHashCode();

    /// <summary>
    /// Determines whether the current and another Core Audio object are pointing to the same Core Audio object.
    /// </summary>
    /// <param name="obj">The other Core Audio object to compare against.</param>
    /// <returns>A value whether the <paramref name="obj"/> is non-null and the instances are pointing to the same Core Audio object.</returns>
    public bool Equals(AudioObject obj) => obj is not null && objectId == obj.objectId;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is AudioObject interopObject && objectId == interopObject.objectId;

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name} of handle 0x{objectId.GetHashCode()}";
}