/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioObject Property Selectors <br />
/// AudioObjectPropertySelector values provided by objects of the AudioObject class.
/// </summary>
/// <remarks>
/// The AudioObject class is the base class for all classes. 
/// As such, all classes inherit this set of properties.
/// </remarks>
internal static class AudioObjectProperties
{
    /// <summary>
    /// An AudioClassID that identifies the class from which the class of the AudioObject is derived. 
    /// This value must always be one of the standard classes.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyBaseClass            = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("bcls");
    /// <summary>
    /// An AudioClassID that identifies the class of the AudioObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyClass                = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("clas");
    /// <summary>
    /// An AudioObjectID that identifies the the AudioObject that owns the given AudioObject. 
    /// Note that all AudioObjects are owned by some other AudioObject.
    /// The only exception is the AudioSystemObject, for which the value of this property is kAudioObjectUnknown.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyOwner                = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("stdv");
    /// <summary>
    /// A CFString that contains the human readable name of the object. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyName                 = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lnam");
    /// <summary>
    /// A CFString that contains the human readable model name of the object. 
    /// The model name differs from kAudioObjectPropertyName in that two objects
    /// of the same model will have the same value for this property but may 
    /// have different values for kAudioObjectPropertyName.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyModelName            = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lmod");
    /// <summary>
    /// A CFString that contains the human readable name of the manufacturer of the hardware the AudioObject is a part of. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyManufacturer         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lmak");
    /// <summary>
    /// A CFString that contains a human readable name for the given element in the given scope. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyElementName          = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lchn");
    /// <summary>
    /// A CFString that contains a human readable name for the category of the given element in the given scope. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyElementCategoryName  = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lccn");
    /// <summary>
    /// A CFString that contains a human readable name for the number of the given element in the given scope. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyElementNumberName    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("lcnn");
    /// <summary>
    /// An array of AudioObjectIDs that represent all the AudioObjects owned by the given object. 
    /// The qualifier is an array of AudioClassIDs. 
    /// If it is non-empty, the returned array of AudioObjectIDs will only refer to objects
    /// whose class is in the qualifier array or whose is a subclass of one in the qualifier array.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyOwnedObjects         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("ownd");
    /// <summary>
    /// A UInt32 where a value of one indicates that the object's hardware is drawing attention to itself, 
    /// typically by flashing or lighting up its front panel display. 
    /// A value of 0 indicates that this function is turned off.
    /// This makes it easy for a user to associate the physical hardware with its representation in an application.
    /// Typically, this property is only supported by AudioDevices and AudioBoxes.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyIdentify             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("iden");
    /// <summary>
    /// A CFString that contains the human readable serial number for the object.
    /// This property will typically be implemented by AudioBox and AudioDevice objects. 
    /// Note that the serial number is not defined to be unique in the same way that an AudioBox's or AudioDevice's UID property are defined. 
    /// This is purely an informational value. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertySerialNumber         = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("snum");
    /// <summary>
    /// A CFString that contains the human readable firmware version for the object. 
    /// This property will typically be implemented by AudioBox and AudioDevice objects. 
    /// Note that this is purely an informational value. 
    /// The caller is responsible for releasing the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioObjectPropertyFirmwareVersion      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("fwvn");
}