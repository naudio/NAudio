
using System;

using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides various global information about the Audio File Services of the 
/// Audio Toolbox framework.
/// </summary>
public static class AudioFileLibraryInformation
{
    private static unsafe string[] GetStringArrayAsResult(AudioFilePropertyID property)
    {
        IntPtr cfArrayHandle;
        uint size = (uint)IntPtr.Size;
        AudioFileException.ThrowIfError(
            NativeMethods.AudioFileGetGlobalInfo(
                property,
                0U, IntPtr.Zero,
                ref size,
                new(&cfArrayHandle)
            )
        );
        using CoreFoundationApi.CFArray array = new(cfArrayHandle);
        string[] strings = new string[array.Count];
        long I = 0L;
        foreach (IntPtr strValue in array.GetItemsAsEnumerable())
        {
            using CoreFoundationApi.CFString strInterop = new(strValue, true);
            strings[I++] = strInterop.ToString();
        }
        return strings;
    }

    private static unsafe string[] GetStringArrayAsResult<T>(AudioFilePropertyID property, Span<T> specifier)
        where T : unmanaged
    {
        IntPtr cfArrayHandle;
        uint size = (uint)IntPtr.Size;
        fixed (T* specifierPointer = specifier)
        {
            AudioFileException.ThrowIfError(
                NativeMethods.AudioFileGetGlobalInfo(
                    property,
                    (uint)(specifier.Length * sizeof(T)),
                    new(specifierPointer),
                    ref size,
                    new(&cfArrayHandle)
                )
            );
        }
        using CoreFoundationApi.CFArray array = new(cfArrayHandle);
        string[] strings = new string[array.Count];
        long I = 0L;
        foreach (IntPtr strValue in array.GetItemsAsEnumerable())
        {
            using CoreFoundationApi.CFString strInterop = new(strValue, true);
            strings[I++] = strInterop.ToString();
        }
        return strings;
    }

    private static unsafe AudioFileTypeID[] GetAudioFileTypeIDArrayAsResult(AudioFilePropertyID property)
    {
        AudioFileException.ThrowIfError(
            NativeMethods.AudioFileGetGlobalInfoSize(property, 0U, IntPtr.Zero, out uint size)
        );
        AudioFileTypeID[] retArray = new AudioFileTypeID[size / sizeof(AudioFileTypeID)];
        fixed (AudioFileTypeID* arrayPointer = retArray)
        {
            AudioFileException.ThrowIfError(
                NativeMethods.AudioFileGetGlobalInfo(
                    property,
                    0U,
                    IntPtr.Zero,
                    ref size,
                    new(arrayPointer)
                )
            );
        }
        Array.Resize(ref retArray, (int)(size / sizeof(AudioFileTypeID)));
        return retArray;
    }

    private static unsafe AudioFileTypeID[] GetAudioFileTypeIDArrayAsResult<T>(AudioFilePropertyID property, Span<T> specifier)
        where T : unmanaged
    {
        uint size;
        uint specifierSize = (uint)(specifier.Length * sizeof(T));
        AudioFileTypeID[] retArray;
        fixed (T* specifierPointer = specifier)
        {
            AudioFileException.ThrowIfError(
                NativeMethods.AudioFileGetGlobalInfoSize(
                    property,
                    specifierSize,
                    new(specifierPointer),
                    out size
                )
            );
            retArray = new AudioFileTypeID[size / sizeof(AudioFileTypeID)];
            fixed (AudioFileTypeID* arrayPointer = retArray)
            {
                AudioFileException.ThrowIfError(
                    NativeMethods.AudioFileGetGlobalInfo(
                        property,
                        specifierSize,
                        new(specifierPointer),
                        ref size,
                        new(arrayPointer)
                    )
                );
            }
        }
        Array.Resize(ref retArray, (int)(size / sizeof(AudioFileTypeID)));
        return retArray;
    }

    internal static string[] GetMimeTypesForFileTypeID(AudioFileTypeID id) => GetStringArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_MIMETypesForType, [id]);

    internal static AudioFileTypeID[] GetTypesForExtension(string value)
    {
        using CoreFoundationApi.CFString cFString = CoreFoundationApi.CFString.CreateFromString(value);
        return GetAudioFileTypeIDArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_TypesForExtension, [cFString.NativeObject]);
    }

    internal static AudioFileTypeID[] GetFileTypeIDsForMimeType(string value)
    {
        using CoreFoundationApi.CFString cFString = CoreFoundationApi.CFString.CreateFromString(value);
        return GetAudioFileTypeIDArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_TypesForMIMEType, [cFString.NativeObject]);
    }

    internal static unsafe AudioStreamBasicDescription[] GetAvailableStreamDescriptionsForFormat(AudioFileTypeAndFormatID fileTypeAndFormatID)
    {
        uint specifierSize = (uint)sizeof(AudioFileTypeAndFormatID);
        AudioFileException.ThrowIfError(
            NativeMethods.AudioFileGetGlobalInfoSize(
                GlobalAudioFileProperties.kAudioFileGlobalInfo_AvailableStreamDescriptionsForFormat,
                specifierSize,
                new(&fileTypeAndFormatID),
                out var size
            )
        );
        AudioStreamBasicDescription[] descriptions = new AudioStreamBasicDescription[size / sizeof(AudioStreamBasicDescription)];
        fixed (AudioStreamBasicDescription* descPointer = descriptions)
        {
            AudioFileException.ThrowIfError(
                NativeMethods.AudioFileGetGlobalInfo(
                    GlobalAudioFileProperties.kAudioFileGlobalInfo_AvailableStreamDescriptionsForFormat,
                    specifierSize,
                    new(&fileTypeAndFormatID),
                    ref size,
                    new(descPointer)
                )
            );
        }
        Array.Resize(ref descriptions, (int)(size / sizeof(AudioStreamBasicDescription)));
        return descriptions;
    }

    /// <summary>
    /// Gets all the MIME types supported by the Audio File Services.
    /// </summary>
    public static string[] SupportedMimeTypes => GetStringArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_AllMIMETypes);

    /// <summary>
    /// Gets all the file extensions recognized by the Audio File Services. <br />
    /// Note that the elements of the returned array do not have a dot prepended,
    /// unlike the <see cref="System.IO"/> API's.
    /// </summary>
    public static string[] RecognizedFileExtensions => GetStringArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_AllExtensions);
}