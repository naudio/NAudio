## NAudio 3 — macOS wrappers: architecture and design

How the NAudio 3 macOS wrappers were put together and which were
the goals for this effort. This document aims to capture
those decisions made during it's development, and
what future contributors to this assembly should notice
before making any changes to. This document is internal.
For using this project, see the publicly provided documentation.

## 1. Goals

Provide support for macOS native audio API's:

- **Playback/Recording support through Core Audio**: Provide all 
the necessary wrappers and logic to perform playback/record 
and connect it with the NAudio library logic.

- **Resampling through the Audio Converter of the Audio Toolbox**:
Provide the platform's default resampler to clients, so that
they can resample their audio data.
It does support almost all the possible audio data conversions,
including channel matrix conversions.
By the time of writing, it is also required by the playback wrapper.

- **Read/Write audio files through the Extended Audio File Services of the Audio Toolbox**:
Provide the platform's API's for reading/writing files.
The big advantage of this API is that it offers us any PCM format since the data
are provided to us via the Audio Converter but it is faster because the converter
is managed by those API's.

## 2. Where things live

All the code is living on the `NAudio.MacOS` project; it contains logic and wrappers for:

- **Core Foundation**: Internally defined wrappers, provides basic system objects 
(such as `CFString` for passing strings to other native API's, 
`CFURL` for translating URL's wherever needed, 
and `CFArray` for accessing arrays of other Core Foundation objects)

- **Core Audio (HAL)**: Wrappers for the low-level Audio Hardware Abstraction Layer
that is the primary means for playback and recording on macOS. It provides a large
wrapper surface to users, and it is based on the object-centric approach that
is declared in the native headers.

- **Audio Toolbox**: Wrappers for the Audio Converter, that provides the 
resampler, and the Extended Audio File Services API, that provides
the platform's audio file readers and writers.

## 3. Understanding macOS API's and libraries.

In macOS, things are radically different in where the native libraries
live and how the native code interacts with them.

Although as a Unix system in it's core, most of the libraries live in two
locations:

- The protected `/usr/lib` folder - most of the library code pertaining to Unix itself and macOS basic services 
is living here.

- The `/System/Library/Frameworks` folder - framework libraries provided by Apple directly for 
applications to code against macOS - graphics, audio (in our case), basic building types, web API's, and others.

The libraries we need are 3, and are all framework libraries, meaning that 
they are living in the `/System/Library/Frameworks` folder,
and are:

- `CoreFoundation.framework`: The Core Foundation library.

- `CoreAudio.framework`: The Core Audio (HAL) library.

- `AudioToolbox.framework`: The Audio Toolbox library.

To manage to get imports from these framework libraries is not 
straightforward though, because these are *folders*, not executable
files. The actual executable file lives into a folder of this format:

```
/Versions/<VERSION_LETTER>/<LIBRARY_NAME>
```

Where `<VERSION_LETTER>` an arbitrary english upper-case character of an internal library version, such as `A`.

And where `<LIBRARY_NAME>` the actual name of the library.

But here is something that works for us: There is always a symbolic link to the latest version of that library,
located in the root folder of the framework folder. So we use that. 

So, for Core Foundation framework the path is this: `/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation`.

### About function calling conventions

We assume that the calling convention of all the functions, as well as for the function pointers
that is C. However, it is to be noted down that Apple does not explicitly mark their exports
and their function pointer definitions over what calling convention they are, but is seems 
that all the API's are built against it.
Probably it is something pertaining to the Apple's Clang compiler.

## 4. Architecture

Architecture of the code was built in six different directions:

1. Provide raw performance on critical audio paths; as such, use `Span` API's for those cases.

2. Be explicit about the interoperability features; use `IDisposable` only on objects need 
to be freed, use `fixed` instead of allocating native where possible, 
do stack allocation for small sizes, to avoid memory leaks as possible.

3. API's that are complex and not required for normal use with other NAudio counterparts
live in `NAudio.MacOS` namespace, any wrapper used for NAudio is in `NAudio.Wave` namespace.

4. All interop signatures must be explicit and match bit-perfectly to the original
native header declaration. Any used structures, type definitions and enumerations 
must also match the native headers bit-perfectly, if possible and not restricted
by the runtime's limitations.
Additionally, full documentation must be provided to these interop signatures 
so that future contributors that continue to develop the assembly can easily 
modify it and understand what each signature does.

5. No macOS-specific special cases must be exported to public API; for example,
the `AudioStreamBasicDescription` is a different definition compared to `WAVEFORMATEX` of Windows,
and instead internal conversion functions convert between the two data formats as needed,
so that to comply with the existing NAudio `WaveFormat` class.

6. Do not use any Swift/Objective-C translation support for now;
We might need in the future such support (loopback recording is one of them), we defer it for now 
until the current API matures and see later on what we will do.

## 5. Core Audio (HAL)

As metioned earlier above, the Core Audio framework provides the API that macOS
processes communicate over the actual audio hardware attached to a macOS device.

All the processes are given a single instance of the audio system object,
which provides the base services and the rudimentary API's for someone 
to configure and discover audio devices.

For the NAudio wrappers, there is a class named as `AudioSystemObject`, 
that contains a static read-only instance field of the aforementioned class,
and it is the only way to access the HAL.

The class offers the process-specific settings the HAL will use 
during several key interactions with the API.

### Playback/Recording

To actually provide or retrieve data to/from an a
ctual hardware device, the HAL offers us the I/O procedure.

In it's core, the I/O procedure is just a callback that is
periodically dispatched on the device's I/O thread.

The I/O procedure needs an audio device object, (obtainable through the `AudioSystemObject` API)
and the callback implementation to be periodically called.

This, however, imposes some limitations that we do not have on WASAPI:

1. We do not manage the thread that does I/O, because the thread is spawned by the device driver
once a device has a valid I/O procedure attached to it, with special threading semantics
that are only available through the macOS dispatch API.

2. While we can know the average latency of the audio device, we cannot modify it.
The only closest to it is the `IOCycleUsage` property on the audio device object,
which it indicates the time to allocate for all the I/O procedures declared on the process.

3. There is no clean `Pause` state. This is HAL design specific because 
its I/O procedures simply transfer data to/from it, and then performs 
the transactions to the hardware device; as such, no bufferring 
involved and a `Pause` state becomes effectively useless.
Note that the `CoreAudioPlayer` API, which is the de-facto NAudio
player wrapper implementation for macOS, honours this peculiarity
by just forwarding the `Pause` call to the `Stop` call, instead of throwing an exception.
However, it is to be noted here that the `PlaybackState` property
will never return the `Paused` value, due to the beforementioned HAL API design.

4. By the time of writing this, loopback recording and process specific capture 
is completely out of scope for the NAudio wrappers, because the 
`CATapDescription` is an Objective-C class, which requires us
to change the TFM to `macos` and also initializing Objective-C runtime
support in the .NET runtime, which is currently a bit hacky. Probably
deferring until Swift support is fully brought into the runtime,
which will be later .NET 12 or so as it seems.


## 6. Audio Toolbox

The Audio Toolbox framework library provides the building blocks 
for providing resampling and audio file manipulation API's. Each API 
is documented below.

### Audio Converter API

Part of the Audio Converter API of the Audio Toolbox is exposed through the 
`MacAudioConverter` class, which is only exposed for performing resampling
services.
It's resampler supports the following:

- Addition and removal of channels, when the source and target wave format `Channels` property do not match.
- Sample rate conversion, with a lot of options to choose from: Quality, resampling algorithm and dithering (dithering is macOS only).
- Conversion between any pair of the following formats:
    - 8 bit integer, signed or unsigned
    - 16, 24, or 32-bit integer.
    - 32 and 64-bit float.

### Extended Audio File Services API

Extended Audio File Services is the primary means for reading and writing audio 
files in macOS. What this API features is that the format can be converted
at will, and as such the wrappers convert the file's data to PCM.

Under the hood, it uses both the low-level Audio File API's and the Audio
Converter API's to achieve this. This also allows us to specify any 
arbitrary PCM format to de/encode file(s) into.

It can open files both from a file path and a stream,
and provides support for any MIME type that is available from the API.

In fact, the supported file types can be queried. 
See the `AudioFileLibraryInformation` class for more information.

## 7. For the future...

1. As mentioned above, loopback recording and process audio capture 
is for now deferred because they require more technical debt and effort
to be ported.
Will wait until .NET releases a fully functional Swift interop interface,
which it would be probably easier to work with.

2. MIDI device interop is not yet brought into the table.
There is the `CoreMIDI` framework library providing such support.
Deferring due to the large API surface already defined - will wait to stabilize
these wrappers, then will look to other stuff as well.

3. For NAudio 3 this support is in preview as it will mature in the next 
months from consumers of the assembly. Probably it will reach stability
after a long time or when NAudio 4 development commences.
