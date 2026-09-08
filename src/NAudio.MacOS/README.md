# NAudio.MacOS

Provides support for macOS playback, recording and file reading/writing through macOS native API's.

NOTE: This package is still in pre-release phase and breaking code changes may be performed as this support is evolved.

Use it at your own risk.

## What's included

- [**Core Audio Framework**](https://developer.apple.com/documentation/coreaudio?language=objc): 
The audio HAL API. Through this API, playback and recording from an installed hardware device is possible.
It provides a managed wrapper, as well as the `CoreAudioPlayer` and `CoreAudioRecorder` classes (at `NAudio.Wave`)
for typical playback and recording scenarios, and are the default implementations that NAudio users
are expected to use.

- [**Audio Toolbox: Audio Converter Services**](https://developer.apple.com/documentation/audiotoolbox/audio-converter-services?language=objc): API providing the macOS resampler.

- [**Audio Toolbox: Extended Audio File Services**](https://developer.apple.com/documentation/audiotoolbox/extended-audio-file-services?language=objc): API's similar to Media Foundation Source Reader and Sink Writer on Windows. They allow to read and write audio files. They support MP3 (read-only), MP4/AAC, OGG Vorbis (read-only) and FLAC.

## When to use it

Use this package whenever you want to do playback or recording on macOS,
and/or you want to use the macOS native audio file reader/writer.

See the [NAudio documentation site](https://naudio.github.io/NAudio/) for tutorials and the full API reference, or the [GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## Acknowledgements

The interop definitions and API documentation comments in this package are derived from
Apple's public framework headers (Core Audio, Audio Toolbox and Core Foundation) and 
from Apple's developer documentation, available at https://developer.apple.com/documentation . 
Individual interoperation source files explicitly note the header(s) they correspond to.

## License

MIT.