/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

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