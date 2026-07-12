/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// An AudioObjectPropertyElement is an integer that identifies, along with the
/// AudioObjectPropertySelector and AudioObjectPropertyScope, a specific piece of
/// information about an AudioObject.
/// </summary>
/// <remarks>
/// The element selects one of possibly many items in the section of the object in
/// which to look for the property. 
/// Elements are number sequentially where 0 represents the main element. 
/// Elements are particular to an instance of a class, meaning that two instances 
/// can have different numbers of elements in the same scope. 
/// There is no inheritance of elements.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioObjectPropertyElement
{
    public static AudioObjectPropertyElement Main => new(0U);

    public readonly uint Value;

    public AudioObjectPropertyElement(uint value) => Value = value;
}