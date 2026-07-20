/*!
	@file		AudioConverter.h
	@framework	AudioToolbox.framework
	@copyright	(c) 1985-2015 by Apple, Inc., all rights reserved.
    @abstract   API's to perform audio format conversions.
    
	AudioConverters convert between various linear PCM and compressed
	audio formats. Supported transformations include:

	- PCM float/integer/bit depth conversions
	- PCM sample rate conversion
	- PCM interleaving and deinterleaving
	- encoding PCM to compressed formats
	- decoding compressed formats to PCM

	A single AudioConverter may perform more than one
	of the above transformations.
*/

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Quality constants for audio converters <br />
/// Constants to be used with <see cref="Wave.MacAudioConverter.Quality"/> property.
/// </summary>
public enum AudioConverterQuality : uint
{
    /// <summary>maximum quality</summary>
    Max = 0x7F,
    /// <summary>high quality</summary>
    High = 0x60,
    /// <summary>medium quality</summary>
    Medium = 0x40,
    /// <summary>low quality</summary>
    Low = 0x20,
    /// <summary>minimum quality</summary>
    Min = 0
}