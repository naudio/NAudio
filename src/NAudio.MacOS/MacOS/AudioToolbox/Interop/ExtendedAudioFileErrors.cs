// This interop definition was derived from the file ExtendedAudioFile.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/audiotoolbox for more information.

#pragma warning disable IDE0055 // We want the error constants to have a consistent view.

namespace NAudio.MacOS.AudioToolbox.Interop;

internal static class ExtendedAudioFileErrors
{
    public const int kExtAudioFileError_InvalidProperty             = -66561;
    public const int kExtAudioFileError_InvalidPropertySize         = -66562;
    public const int kExtAudioFileError_NonPCMClientFormat          = -66563;
    public const int kExtAudioFileError_InvalidChannelMap           = -66564;    // number of channels doesn't match format
    public const int kExtAudioFileError_InvalidOperationOrder       = -66565;
    public const int kExtAudioFileError_InvalidDataFormat           = -66566;
    public const int kExtAudioFileError_MaxPacketSizeUnknown        = -66567;
    public const int kExtAudioFileError_InvalidSeek                 = -66568;    // writing, or offset out of bounds
    public const int kExtAudioFileError_AsyncWriteTooLarge          = -66569;
    public const int kExtAudioFileError_AsyncWriteBufferOverflow    = -66570;    // an async write could not be completed in time
}