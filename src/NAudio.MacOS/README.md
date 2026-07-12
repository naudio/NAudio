# NAudio.MacOS

Provides support for macOS playback, recording and file reading/writing through macOS native API's.

Note that this package is still in pre-release phase and breaking code changes may take place as this support is evolved. Use it at your own risk.

The assembly/package contains wrappers to:

- [Core Audio Framework](https://developer.apple.com/documentation/coreaudio?language=objc): The audio HAL API. Through this API, playback and recording is possible.

- [Audio Toolbox: Extended Audio File Services](https://developer.apple.com/documentation/audiotoolbox/extended-audio-file-services?language=objc): API's similar to Media Foundation Source Reader and Sink Writer on Windows. They allow to read and write audio files.

## When to use it

Use this package whenever you want to use playback/recording or reading/writing audio files in macOS.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and tutorials.

## License

MIT.