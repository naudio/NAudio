
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Special exception type thrown when a specified Core Audio framework property cannot be located by the library.
/// </summary>
public sealed class CoreAudioPropertyNotFoundException : CoreAudioException
{
    /// <summary>
    /// Constructs a new instance of the <see cref="CoreAudioPropertyNotFoundException"/> class,
    /// specifying the property's name that was not found.
    /// </summary>
    /// <param name="propertyName">The property name, as an identifier.</param>
    public CoreAudioPropertyNotFoundException(uint propertyName)
        : base(ErrorConstants.kAudioHardwareUnknownPropertyError)
    {
        Data.Add("PropertyName", propertyName);
    }
}