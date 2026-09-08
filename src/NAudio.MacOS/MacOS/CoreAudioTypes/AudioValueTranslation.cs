// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// This stucture holds the buffers necessary for translation operations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioValueTranslation
{
    /// <summary>
    /// The buffer containing the data to be translated.
    /// </summary>
    [NotNull]
    public readonly IntPtr mInputData;
    /// <summary>
    /// The number of bytes in the buffer pointed at by mInputData.
    /// </summary>
    public readonly UInt32 mInputDataSize;
    /// <summary>
    /// The buffer to hold the result of the translation.
    /// </summary>
    [NotNull]
    public readonly IntPtr mOutputData;
    /// <summary>
    /// The number of bytes in the buffer pointed at by mOutputData.
    /// </summary>
    public readonly UInt32 mOutputDataSize;
}