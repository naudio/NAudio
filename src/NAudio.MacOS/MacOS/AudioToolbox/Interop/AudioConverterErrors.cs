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

#pragma warning disable IDE0055 // We want the constants to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.AudioToolbox.Interop;

internal static class AudioConverterErrors
{
    public static readonly int kAudioConverterErr_FormatNotSupported              = MacUtils.ConstructIntConstantValueFromString("fmt?");
    public static readonly int kAudioConverterErr_OperationNotSupported           = MacUtils.ConstructIntConstantValueFromString("op??"); // 0x6F703F3F, // 'op??"); integer used because of trigraph
    public static readonly int kAudioConverterErr_PropertyNotSupported            = MacUtils.ConstructIntConstantValueFromString("prop");
    public static readonly int kAudioConverterErr_InvalidInputSize                = MacUtils.ConstructIntConstantValueFromString("insz");
    public static readonly int kAudioConverterErr_InvalidOutputSize               = MacUtils.ConstructIntConstantValueFromString("otsz");
        // e.g. byte size is not a multiple of the frame size
    public static readonly int kAudioConverterErr_UnspecifiedError                = MacUtils.ConstructIntConstantValueFromString("what");
    public static readonly int kAudioConverterErr_BadPropertySizeError            = MacUtils.ConstructIntConstantValueFromString("!siz");
    public static readonly int kAudioConverterErr_RequiresPacketDescriptionsError = MacUtils.ConstructIntConstantValueFromString("!pkd");
    public static readonly int kAudioConverterErr_InputSampleRateOutOfRange       = MacUtils.ConstructIntConstantValueFromString("!isr");
    public static readonly int kAudioConverterErr_OutputSampleRateOutOfRange      = MacUtils.ConstructIntConstantValueFromString("!osr");
}