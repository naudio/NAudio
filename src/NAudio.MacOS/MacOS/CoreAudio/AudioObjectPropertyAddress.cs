// This interop definition was derived from the file AudioHardwareBase.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudio;

/*!
    @struct         AudioObjectPropertyAddress
    @abstract       An AudioObjectPropertyAddress collects the three parts that identify a specific
                    property together in a struct for easy transmission.
    @field          mSelector
                        The AudioObjectPropertySelector for the property.
    @field          mScope
                        The AudioObjectPropertyScope for the property.
    @field          mElement
                        The AudioObjectPropertyElement for the property.
*/
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioObjectPropertyAddress
{
    public readonly AudioObjectPropertySelector mSelector;
    public readonly AudioObjectPropertyScope mScope;
    public readonly AudioObjectPropertyElement mElement;

    public AudioObjectPropertyAddress(AudioObjectPropertySelector selector, AudioObjectPropertyScope scope, AudioObjectPropertyElement element)
    {
        mScope = scope;
        mElement = element;
        mSelector = selector;
    }

    public static AudioObjectPropertyAddress CreateWithGlobalScopeAndMainElement(AudioObjectPropertySelector selector)
        => CreateWithScopeAndMainElement(selector, AudioObjectPropertyScopeConstants.Global);

    public static AudioObjectPropertyAddress CreateWithScopeAndMainElement(AudioObjectPropertySelector selector, AudioObjectPropertyScope scope)
        => new(selector, scope, AudioObjectPropertyElement.Main);

    public override int GetHashCode() => HashCode.Combine(mSelector, mScope, mElement);

    public override bool Equals(object obj) => obj is AudioObjectPropertyAddress addr && addr == this;

    public static bool operator ==(AudioObjectPropertyAddress addr1, AudioObjectPropertyAddress addr2) =>
        addr1.mElement.Value == addr2.mElement.Value &&
        addr1.mScope == addr2.mScope &&
        addr1.mSelector == addr2.mSelector;

    public static bool operator !=(AudioObjectPropertyAddress addr1, AudioObjectPropertyAddress addr2) => !(addr1 == addr2);
}