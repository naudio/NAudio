NAudio is an open source .NET audio library written by [Mark Heath](https://markheath.net)

## Documentation

We're currently in the process of migrating documentation from [CodePlex](http://naudio.codeplex.com). 

 - [Getting Started](Docs/GettingStarted.md)
 - [Playing an Audio File from a WinForms application](Docs/GettingStarted.md)
 - [Playing an Audio File from a Console application](Docs/PlayAudioConsoleApp.md)

Additional sources of documentation for NAudio are:
 - [Original Documentation on CodePlex](http://naudio.codeplex.com/documentation)
 - [NAudio articles on Mark Heath's blog](http://markheath.net/category/naudio)
 - [Pluralsight Course - Audio Programming with NAudio](https://app.pluralsight.com/library/courses/audio-programming-naudio/table-of-contents) (subscribers only I'm afraid)



## Features

* Play back audio using a variety of APIs
    * WaveOut
    * DirectSound
    * ASIO
    * WASAPI (Windows Vista and above)
* Read audio from many standard file formats
    * WAV 
    * AIFF
    * WMA
    * SoundFont files (SF2)
* Decode many popular audio compression types
    * MP3 (using ACM, DMO or MFT)
    * G.711 mu-law and a-law
    * ADPCM
    * G.722
    * Speex (using NSpeex)
    * WMA, AAC, MP4 and more others with Media Foundation
* Convert between various forms of uncompressed audio
    * Change the number of channels - Mono to stereo, stereo to mono
    * Modify bit depth (8, 16, 24, 32 integer or 32 bit IEEE float)
    * Resample audio using a choice of resampling algorithms
* Encode audio using any ACM or Media Foundation codec installed on your computer
    * Create MP3s on Windows 8 and above
    * Create AAC/MP4 audio on Windows 7 and above
    * Create WMA files
    * Create WAV files containing G.711, ADPCM, G.722, etc.
* Mix and manipulate audio streams using a 32-bit floating mixing engine
    * construct signal chains 
    * examine sample levels for the purposes of metering or waveform rendering
    * pass blocks of samples through an FFT for metering or DSP
    * delay, loop, or fade audio in and out
    * Perform EQ with a BiQuad filter (allowing low pass, high pass, peaking EQ, etc.)
    * Pitch shifting of audio with a phase vocoder
* Record audio 
    * using WaveIn, WASAPI or ASIO
* Work with soundcards
    * Enumerate devices
    * Access soundcard controls and metering information
* Full MIDI event model
    * Read and write MIDI files
    * Respond to received MIDI events
    * Send MIDI events
* An extensible model
    * All base classes easily inherited from for you to add your custom components
* Support for Windows RT
    * Create Windows 8 Store apps and Windows Universal apps
