
using System;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Collections.Immutable;

using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides various global information about the Audio File Services of the 
/// Audio Toolbox framework.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.3")]
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
        uint specifierSize = (uint)(specifier.Length * (long)sizeof(T));
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

    private static string[] GetMimeTypesForFileTypeID(AudioFileTypeID id) => GetStringArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_MIMETypesForType, [id]);

    private static string[] GetFileExtensionsForFileTypeID(AudioFileTypeID id) => GetStringArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_ExtensionsForType, [id]);

    internal static AudioFileTypeID[] GetTypesForExtension(string value)
    {
        using var stringNative = CoreFoundationApi.CFString.CreateFromString(value);
        return GetAudioFileTypeIDArrayAsResult(
            GlobalAudioFileProperties.kAudioFileGlobalInfo_TypesForExtension,
            // No need to use the NativeObject property here,
            // as we created the CFString instance ourselves above.
            [stringNative.DangerousGetObject()]
        );
    }

    internal static AudioFileTypeID[] GetFileTypeIDsForMimeType(string value)
    {
        using var stringNative = CoreFoundationApi.CFString.CreateFromString(value);
        return GetAudioFileTypeIDArrayAsResult(
            GlobalAudioFileProperties.kAudioFileGlobalInfo_TypesForMIMEType,
            // No need to use the NativeObject property here,
            // as we created the CFString instance ourselves above.
            [stringNative.DangerousGetObject()]
        );
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

    /// <summary>
    /// Gets all the MIME types that are readable by the Audio File Services.
    /// </summary>
    public static ImmutableArray<string> SupportedReadableMimeTypes
    {
        get
        {
            var readableTypes = GetAudioFileTypeIDArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_ReadableTypes);
            // Some file types may correspond in one or more MIME types,
            // with some of them sharing the same MIME type.
            // Using a HashSet here avoids those duplicates appearing in user code.
            HashSet<string> mimeTypes = new(readableTypes.Length, StringComparer.Ordinal);
            foreach (var id in readableTypes)
            {
                try
                {
                    mimeTypes.UnionWith(GetMimeTypesForFileTypeID(id));
                }
                catch (NotSupportedException)
                {
                    // Sometimes, the ID reported by the AudioFile API
                    // is not recognized by the MIMETypesForType property,
                    // and throws NotSupportedException, saying that 
                    // the property is not supported.
                    // So, we just ignore it and continue with the next ID. 
                }
            }
            // Note that we deliberately return an ImmutableArray here
            // to avoid exposing the HashSet to user code, which with
            // a simple instance check or cast they could modify the HashSet.
            return mimeTypes.ToImmutableArray();
        }
    }

    /// <summary>
    /// Gets all the MIME types that are writable by the Audio File Services.
    /// </summary>
    public static ImmutableArray<string> SupportedWritableMimeTypes
    {
        get
        {
            var writableTypes = GetAudioFileTypeIDArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_WritableTypes);
            // Some file types may correspond in one or more MIME types,
            // with some of them sharing the same MIME type.
            // Using a HashSet here avoids those duplicates appearing in user code.
            HashSet<string> mimeTypes = new(writableTypes.Length, StringComparer.Ordinal);
            foreach (var id in writableTypes)
            {
                try
                {
                    mimeTypes.UnionWith(GetMimeTypesForFileTypeID(id));
                }
                catch (NotSupportedException)
                {
                    // Sometimes, the ID reported by the AudioFile API
                    // is not recognized by the MIMETypesForType property,
                    // and throws NotSupportedException, saying that 
                    // the property is not supported.
                    // So, we just ignore it and continue with the next ID.
                }
            }
            // Note that we deliberately return an ImmutableArray here
            // to avoid exposing the HashSet to user code, which with
            // a simple instance check or cast they could modify the HashSet.
            return mimeTypes.ToImmutableArray();
        }
    }

    /// <summary>
    /// Gets all the file extensions that are readable by the Audio File Services. <br />
    /// Note that the elements of the returned array do not have a dot prepended,
    /// unlike the <see cref="System.IO"/> API's.
    /// </summary>
    public static ImmutableArray<string> RecognizedReadableFileExtensions
    {
        get
        {
            var readableTypes = GetAudioFileTypeIDArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_ReadableTypes);
            // Some file types may correspond in one or more file extensions,
            // with some of them sharing the same extension.
            // Using a HashSet here avoids those duplicates appearing in user code.
            HashSet<string> extensions = new(readableTypes.Length, StringComparer.Ordinal);
            foreach (var id in readableTypes)
            {
                try
                {
                    extensions.UnionWith(GetFileExtensionsForFileTypeID(id));
                }
                catch (NotSupportedException)
                {
                    // Sometimes, the ID reported by the AudioFile API
                    // is not recognized by the ExtensionsForType property,
                    // and throws NotSupportedException, saying that 
                    // the property is not supported.
                    // So, we just ignore it and continue with the next file type.
                }
            }
            // Note that we deliberately return an ImmutableArray here
            // to avoid exposing the HashSet to user code, which with
            // a simple instance check or cast they could modify the HashSet.
            return extensions.ToImmutableArray();
        }
    }

    /// <summary>
    /// Gets all the file extensions that are writable by the Audio File Services. <br />
    /// Note that the elements of the returned array do not have a dot prepended,
    /// unlike the <see cref="System.IO"/> API's.
    /// </summary>
    public static ImmutableArray<string> RecognizedWritableFileExtensions
    {
        get
        {
            var writableTypes = GetAudioFileTypeIDArrayAsResult(GlobalAudioFileProperties.kAudioFileGlobalInfo_WritableTypes);
            // Some file types may correspond in one or more file extensions,
            // with some of them sharing the same extension.
            // Using a HashSet here avoids those duplicates appearing in user code.
            HashSet<string> extensions = new(writableTypes.Length, StringComparer.Ordinal);
            foreach (var id in writableTypes)
            {
                try
                {
                    extensions.UnionWith(GetFileExtensionsForFileTypeID(id));
                }
                catch (NotSupportedException)
                {
                    // Sometimes, the ID reported by the AudioFile API
                    // is not recognized by the ExtensionsForType property,
                    // and throws NotSupportedException, saying that 
                    // the property is not supported.
                    // So, we just ignore it and continue with the next file type.
                }
            }
            // Note that we deliberately return an ImmutableArray here
            // to avoid exposing the HashSet to user code, which with
            // a simple instance check or cast they could modify the HashSet.
            return extensions.ToImmutableArray();
        }
    }
}