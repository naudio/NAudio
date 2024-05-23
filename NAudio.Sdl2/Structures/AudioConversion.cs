using System;
using static NAudio.Sdl2.Interop.SDL;

namespace NAudio.Sdl2.Structures
{
    [Flags]
    public enum AudioConversion : uint
    {
        None = 0,
        Frequency = SDL_AUDIO_ALLOW_FREQUENCY_CHANGE,
        Format = SDL_AUDIO_ALLOW_FORMAT_CHANGE,
        Channels = SDL_AUDIO_ALLOW_CHANNELS_CHANGE,
        Samples = SDL_AUDIO_ALLOW_SAMPLES_CHANGE,
        Any = SDL_AUDIO_ALLOW_ANY_CHANGE
    }
}
