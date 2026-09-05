using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NAudio.Wave.SampleProviders;

// AOT smoke app. Exercises three directions of the source-generated COM bridging:
//
// (1) RCW direction — IPropertyStore + PropVariant (Phase 2d) and the
//     CoCreateInstance / GetOrCreateObjectForComInstance projection path
//     (Phase 2e). Enumerates render endpoints and reads VT_LPWSTR / VT_UI4 /
//     VT_BLOB properties.
//
// (2) CCW direction — the [GeneratedComClass] callback dispatch reworked in
//     Phase 2f. Subscribes via MMDeviceNotificationClient (which wraps the
//     IMMNotificationClient CCW internally) and an
//     IAudioEndpointVolumeCallback (via AudioEndpointVolume.OnVolumeNotification),
//     drives the master volume a few times, and counts firings. The Phase 2f
//     QI-for-IID fix is tested here under genuine NativeAOT — pure trim wasn't
//     sufficient because trim still permits reflection-based vtable inference;
//     PublishAot does not, so this run is the strongest "callbacks survive
//     whole-program analysis" signal we have.
//
// (3) MediaFoundation round-trip — Phase 2e' (this section). Exercises the
//     MediaFoundationInterop p/invokes (mfplat / mfreadwrite), the bridge sweep
//     in MediaFoundationApi factories, the consumer cascade through
//     MediaFoundationEncoder + StreamMediaFoundationReader, the
//     IMFTransform-backed MediaFoundationResampler, and the ComStream CCW path
//     (Step 5 + Phase 2f H3 QI-for-IID rule). Encodes a generated signal to
//     MP3 in a MemoryStream, then reads it back. If the QI handoff or any of
//     the migrated bridge sites were wrong, MFCreateMFByteStreamOnStream or
//     IMFSourceReader::ReadSample would AV before the assertions ran.
//
// (4) DirectSound playback — Phase 2g (this section). Exercises the three
//     [GeneratedComInterface]-migrated DirectSound interfaces (IDirectSound,
//     IDirectSoundBuffer, IDirectSoundNotify), the QI cascade between
//     IDirectSoundBuffer and IDirectSoundNotify, the [LibraryImport]
//     DirectSoundCreate / DirectSoundEnumerate path, and the
//     [UnmanagedCallersOnly] enumeration thunk. Constructs a DirectSoundOut,
//     drives a brief silent playback, and disposes — issue #1191's failure
//     under PublishTrimmed (StubHelpers.InterfaceMarshaler stripped) is
//     precisely this path; if the migration regressed it, this section would
//     AV before reporting OK.
//
// (5) winmm / MME — issue #1425 (this section). NAudio.WinMM expressed two native
//     types as [StructLayout] classes passed by value: WAVEHDR and WAVEFORMATEX.
//     CoreCLR pins a blittable class argument in place, so the driver wrote its
//     state straight into the managed object and a subclass's inherited fields
//     were laid out correctly. NativeAOT does neither — it copies the argument
//     into a per-call temporary, and it drops the base class fields of a
//     WaveFormat subclass entirely. The result was that every waveInAddBuffer
//     failed with WAVERR_UNPREPARED and every WaveFormatExtensible reached the
//     driver with its SubFormat GUID written over the sample rate. The blob
//     assertions below need no audio hardware and are the regression guard; the
//     WaveOut/WaveIn drives that follow are skipped when there is no device.
//
// (6) ACM — issue #1425 again (this section). ACMSTREAMHEADER was the third native type
//     expressed as a [StructLayout] class passed by value, and #1427 missed it. The codec
//     keeps private state in the header's reserved tail between acmStreamPrepareHeader,
//     acmStreamConvert and acmStreamUnprepareHeader; NativeAOT's per-call copy round-trips
//     the declared fields alone, so that tail came back zeroed and every conversion failed.
//     Mp3FileReader and AudioFileReader both decode through this path. ACM codecs are
//     software, so unlike the WaveOut/WaveIn drives this section needs no audio hardware.

// PublishAot only takes effect on `dotnet publish`; `dotnet run` executes this under the JIT,
// where every path below passed even while it was broken under AOT — which is how issue #1425 went
// unnoticed. Report the runtime actually executing so a green JIT run can't be mistaken for AOT
// validation, and label the per-phase results with it rather than asserting PublishAot regardless.
var runtime = RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "NativeAOT";
Console.WriteLine($"NAudio AOT smoke test — running under {runtime}\n");
if (RuntimeFeature.IsDynamicCodeSupported)
{
    Console.WriteLine("NOTE: this is NOT an AOT validation run. Publish with PublishAot and run the");
    Console.WriteLine("      produced executable instead — see README.md in this folder.\n");
}

Console.WriteLine("=== Phase 2d / 2e: RCW direction (property reads) ===\n");

var enumerator = new MMDeviceEnumerator();
var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

Console.WriteLine($"Found {devices.Count} active render endpoint(s):");
foreach (MMDevice device in devices)
{
    Console.WriteLine();
    Console.WriteLine($"  {device.FriendlyName}");
    Console.WriteLine($"    ID:           {device.ID}");
    Console.WriteLine($"    State:        {device.State}");
    Console.WriteLine($"    DataFlow:     {device.DataFlow}");

    // Hits VT_LPWSTR — guid is stored as a stringified GUID
    if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_GUID))
    {
        Console.WriteLine($"    EndpointGUID: {device.Properties[PropertyKeys.PKEY_AudioEndpoint_GUID].Value}");
    }

    // Hits VT_UI4
    if (device.Properties.Contains(PropertyKeys.PKEY_AudioEndpoint_FormFactor))
    {
        Console.WriteLine($"    FormFactor:   {device.Properties[PropertyKeys.PKEY_AudioEndpoint_FormFactor].Value}");
    }

    // Hits VT_BLOB (WAVEFORMATEX)
    if (device.Properties.Contains(PropertyKeys.PKEY_AudioEngine_DeviceFormat))
    {
        var blob = (byte[])device.Properties[PropertyKeys.PKEY_AudioEngine_DeviceFormat].Value;
        Console.WriteLine($"    DeviceFormat: {blob.Length}-byte WAVEFORMAT blob");
    }

    Console.WriteLine($"    Total properties: {device.Properties.Count}");
}

Console.WriteLine();
Console.WriteLine("=== Phase 2f: CCW direction (callback dispatch) ===\n");

// Endpoint notifications — exercise the high-level MMDeviceNotificationClient (which wraps the
// [GeneratedComClass] CCW internally) under PublishAot. No easy automated trigger for device add/remove;
// just confirm the subscribe/dispose round-trip works. Plug/unplug a USB device while this runs to see
// live notifications. (A worker thread raises these, so keep events off the SynchronizationContext.)
var notifications = enumerator.CreateNotificationClient(useSynchronizationContext: false);
notifications.DeviceStateChanged += (_, e) => Console.WriteLine($"    [notification] DeviceStateChanged {e.NewState} {e.DeviceId}");
notifications.DeviceAdded += (_, e) => Console.WriteLine($"    [notification] DeviceAdded {e.DeviceId}");
notifications.DeviceRemoved += (_, e) => Console.WriteLine($"    [notification] DeviceRemoved {e.DeviceId}");
notifications.DefaultDeviceChanged += (_, e) => Console.WriteLine($"    [notification] DefaultDeviceChanged {e.Flow}/{e.Role} → {e.DeviceId}");
Console.WriteLine("  CreateNotificationClient: OK");

// IAudioEndpointVolumeCallback — drive the default render endpoint and count callbacks.
var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
var endpointVolume = defaultDevice.AudioEndpointVolume;
float originalLevel = endpointVolume.MasterVolumeLevelScalar;
Console.WriteLine($"  Default endpoint: {defaultDevice.FriendlyName} (master={originalLevel:F3})");

int notifyCount = 0;
endpointVolume.OnVolumeNotification += _ => Interlocked.Increment(ref notifyCount);

try
{
    float[] levels = { 0.40f, 0.60f, 0.40f, 0.50f };
    foreach (var lvl in levels)
    {
        endpointVolume.MasterVolumeLevelScalar = lvl;
        Thread.Sleep(75);
    }
    Thread.Sleep(200); // let stragglers land
    Console.WriteLine($"  Drove {levels.Length} master-volume changes, {notifyCount} OnVolumeNotification callbacks fired.");
    Console.WriteLine(notifyCount > 0
        ? $"  CCW dispatch under {runtime}: OK"
        : $"  CCW dispatch under {runtime}: FAIL — zero callbacks fired (registration didn't take or AOT trimmed the dispatch path)");
}
finally
{
    try { endpointVolume.MasterVolumeLevelScalar = originalLevel; } catch { }
    Thread.Sleep(150);
    notifications.Dispose();
    endpointVolume.Dispose();
    defaultDevice.Dispose();
}

Console.WriteLine();
Console.WriteLine("=== Phase 2e': MediaFoundation round-trip (RCW + CCW) ===\n");

MediaFoundationApi.Startup();
try
{
    using var encoded = new MemoryStream();
    var signal = new SignalGenerator(44100, 2) { Frequency = 1000, Gain = 0.25 }
        .Take(TimeSpan.FromSeconds(2));
    MediaFoundationEncoder.EncodeToMp3(signal.ToWaveProvider(), encoded, 96000);
    Console.WriteLine($"  EncodeToMp3 (CCW + RCW): wrote {encoded.Length} bytes to MemoryStream");

    encoded.Position = 0;
    using var reader = new StreamMediaFoundationReader(encoded);
    Console.WriteLine($"  StreamMediaFoundationReader format: {reader.WaveFormat}");

    var buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
    long total = 0;
    int bytesRead;
    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
    {
        total += bytesRead;
    }
    Console.WriteLine($"  StreamMediaFoundationReader read back {total} bytes of PCM");

    // Resampler exercise — IMFTransform path.
    encoded.Position = 0;
    using var reader2 = new StreamMediaFoundationReader(encoded);
    using var resampler = new MediaFoundationResampler(reader2, 22050);
    var resampleBuffer = new byte[resampler.WaveFormat.AverageBytesPerSecond];
    long resampleTotal = 0;
    while ((bytesRead = resampler.Read(resampleBuffer.AsSpan())) > 0)
    {
        resampleTotal += bytesRead;
    }
    Console.WriteLine($"  MediaFoundationResampler 44100->22050: produced {resampleTotal} bytes");

    Console.WriteLine(total > 0 && resampleTotal > 0
        ? $"  MediaFoundation under {runtime}: OK"
        : $"  MediaFoundation under {runtime}: FAIL");
}
finally
{
    MediaFoundationApi.Shutdown();
}

Console.WriteLine();
Console.WriteLine("=== Phase 2g: DirectSound playback (RCW direction + QI cascade) ===\n");

// Enumerate first — exercises the [UnmanagedCallersOnly] EnumCallbackThunk via
// DirectSoundEnumerate's function-pointer callback parameter.
int dsoundDeviceCount = 0;
foreach (var dsoundDevice in DirectSoundOut.Devices)
{
    Console.WriteLine($"  DirectSound device: {dsoundDevice.Description} ({dsoundDevice.Guid})");
    dsoundDeviceCount++;
}
Console.WriteLine($"  Enumerated {dsoundDeviceCount} DirectSound device(s).");

// Drive a brief silent playback — exercises DirectSoundCreate, IDirectSound,
// IDirectSoundBuffer (primary + secondary), and the QI cascade to
// IDirectSoundNotify. A silent SignalGenerator at zero gain keeps CI silent.
using (var dsoundOut = new DirectSoundOut(40))
{
    var silentSignal = new SignalGenerator(44100, 2) { Frequency = 440, Gain = 0.0 }
        .Take(TimeSpan.FromMilliseconds(500))
        .ToWaveProvider();
    dsoundOut.Init(silentSignal);
    dsoundOut.Play();
    Thread.Sleep(250);
    Console.WriteLine($"  DirectSoundOut PlaybackState while playing: {dsoundOut.PlaybackState}");
    dsoundOut.Stop();
    Thread.Sleep(150);
    Console.WriteLine($"  DirectSoundOut PlaybackState after Stop:    {dsoundOut.PlaybackState}");
}
Console.WriteLine($"  DirectSound playback under {runtime}: OK");

Console.WriteLine();
Console.WriteLine("=== Issue #1425: winmm WAVEHDR / WAVEFORMATEX marshalling ===\n");

int winmmFailures = 0;

void Check(string what, bool condition, object? actual)
{
    if (!condition)
    {
        winmmFailures++;
    }
    Console.WriteLine($"  {(condition ? "OK  " : "FAIL")} {what} = {actual}");
}

// Part 1 — the WAVEFORMATEX blob handed to waveOutOpen / waveInOpen / acmStreamOpen.
// Needs no audio hardware, so this part runs everywhere including CI agents.
Console.WriteLine("  WaveFormat.MarshalToPtr blob layout:");

var pcmFormat = new WaveFormat(44100, 16, 2);
IntPtr pcmPtr = WaveFormat.MarshalToPtr(pcmFormat);
try
{
    Check("PCM  sampleRate @4", Marshal.ReadInt32(pcmPtr, 4) == 44100, Marshal.ReadInt32(pcmPtr, 4));
    Check("PCM  bitsPerSample @14", Marshal.ReadInt16(pcmPtr, 14) == 16, Marshal.ReadInt16(pcmPtr, 14));
    Check("PCM  cbSize @16", Marshal.ReadInt16(pcmPtr, 16) == 0, Marshal.ReadInt16(pcmPtr, 16));
}
finally
{
    Marshal.FreeHGlobal(pcmPtr);
}

// The one that actually broke: a subclass's own fields must follow the inherited
// WAVEFORMATEX, not overwrite it.
var extensibleFormat = new WaveFormatExtensible(48000, 24, 2, 0x3);
IntPtr extensiblePtr = WaveFormat.MarshalToPtr(extensibleFormat);
try
{
    Check("EXT  sampleRate @4", Marshal.ReadInt32(extensiblePtr, 4) == 48000, Marshal.ReadInt32(extensiblePtr, 4));
    Check("EXT  bitsPerSample @14", Marshal.ReadInt16(extensiblePtr, 14) == 24, Marshal.ReadInt16(extensiblePtr, 14));
    Check("EXT  cbSize @16", Marshal.ReadInt16(extensiblePtr, 16) == 22, Marshal.ReadInt16(extensiblePtr, 16));
    Check("EXT  wValidBitsPerSample @18", Marshal.ReadInt16(extensiblePtr, 18) == 24, Marshal.ReadInt16(extensiblePtr, 18));
    Check("EXT  dwChannelMask @20", Marshal.ReadInt32(extensiblePtr, 20) == 0x3, Marshal.ReadInt32(extensiblePtr, 20));

    var roundTripped = WaveFormat.MarshalFromPtr(extensiblePtr) as WaveFormatExtensible;
    Check("EXT  round-trips as WaveFormatExtensible", roundTripped != null, roundTripped?.GetType().Name ?? "null");
    Check("EXT  round-tripped SubFormat",
        roundTripped?.SubFormat == extensibleFormat.SubFormat, roundTripped?.SubFormat);
}
finally
{
    Marshal.FreeHGlobal(extensiblePtr);
}

// Part 2 — drive the real devices. waveOutPrepareHeader / waveOutWrite and
// waveInPrepareHeader / waveInAddBuffer are the calls that take the WAVEHDR address.
Console.WriteLine();
Console.WriteLine($"  waveOut devices: {WaveOut.DeviceCount}, waveIn devices: {WaveIn.DeviceCount}");

if (WaveOut.DeviceCount > 0)
{
    try
    {
        using var waveOut = new WaveOut { BufferMilliseconds = 60 };
        var tone = new SignalGenerator(48000, 2) { Frequency = 440, Gain = 0.0 }
            .Take(TimeSpan.FromMilliseconds(600))
            .ToWaveProvider();
        waveOut.Init(tone);
        waveOut.Play();
        Thread.Sleep(300);
        var stateWhilePlaying = waveOut.PlaybackState;
        waveOut.Stop();
        Check("waveOut playback reached Playing", stateWhilePlaying == PlaybackState.Playing, stateWhilePlaying);
    }
    catch (MmException e)
    {
        winmmFailures++;
        Console.WriteLine($"  FAIL waveOut playback threw {e.Result} calling {e.Function}");
    }
}
else
{
    Console.WriteLine("  SKIP waveOut playback — no render device on this machine");
}

if (WaveIn.DeviceCount > 0)
{
    try
    {
        long bytesRecorded = 0;
        Exception? stoppedWith = null;
        using (var waveIn = new WaveIn { WaveFormat = new WaveFormat(44100, 16, 1), BufferMilliseconds = 50 })
        {
            waveIn.DataAvailable += (_, e) => bytesRecorded += e.BytesRecorded;
            waveIn.RecordingStopped += (_, e) => stoppedWith = e.Exception;
            waveIn.StartRecording();
            Thread.Sleep(500);
            waveIn.StopRecording();
            Thread.Sleep(200);
        }

        // Before the fix this is exactly where it blew up: the driver's WHDR_PREPARED never
        // reached the managed header, so the first waveInAddBuffer failed with MMSYSERR 34.
        Check("waveIn recording did not fault", stoppedWith == null, stoppedWith?.Message ?? "no exception");
        Check("waveIn delivered audio", bytesRecorded > 0, $"{bytesRecorded} bytes");
    }
    catch (MmException e)
    {
        winmmFailures++;
        Console.WriteLine($"  FAIL waveIn recording threw {e.Result} calling {e.Function}");
    }
}
else
{
    Console.WriteLine("  SKIP waveIn recording — no capture device on this machine");
}

Console.WriteLine();
Console.WriteLine(winmmFailures == 0
    ? $"  winmm WAVEHDR/WAVEFORMATEX under {runtime}: OK"
    : $"  winmm WAVEHDR/WAVEFORMATEX under {runtime}: FAIL ({winmmFailures} check(s))");

Console.WriteLine();
Console.WriteLine("=== Issue #1425: ACM stream header marshalling ===\n");

int acmFailures = 0;

void AcmCheck(string what, bool condition, object? actual)
{
    if (!condition)
    {
        acmFailures++;
    }
    Console.WriteLine($"  {(condition ? "OK  " : "FAIL")} {what} = {actual}");
}

// A recognisable signal rather than silence, so the checks below can tell a working
// conversion from one that returns the right byte count full of zeroes.
static byte[] GenerateTone(WaveFormat format, int sampleCount)
{
    var bytes = new byte[sampleCount * format.BlockAlign];
    for (int n = 0; n < sampleCount; n++)
    {
        short sample = (short)(Math.Sin(2 * Math.PI * 440 * n / format.SampleRate) * 16000);
        for (int channel = 0; channel < format.Channels; channel++)
        {
            int offset = n * format.BlockAlign + channel * 2;
            bytes[offset] = (byte)(sample & 0xFF);
            bytes[offset + 1] = (byte)((sample >> 8) & 0xFF);
        }
    }
    return bytes;
}

var acmSourceFormat = new WaveFormat(44100, 16, 2);
var acmTargetFormat = new WaveFormat(22050, 16, 1);

// Part 1 — AcmStream directly: the prepare / convert / unprepare sequence that broke.
try
{
    using var acmStream = new AcmStream(acmSourceFormat, acmTargetFormat);
    var tone = GenerateTone(acmSourceFormat, 4410);
    Array.Copy(tone, acmStream.SourceBuffer, tone.Length);

    int converted = acmStream.Convert(tone.Length, out int sourceUsed);
    AcmCheck("AcmStream produced output", converted > 0, $"{converted} bytes");
    AcmCheck("AcmStream consumed input", sourceUsed > 0, $"{sourceUsed} bytes");

    bool anyAudio = false;
    for (int n = 0; n < converted && !anyAudio; n++)
    {
        anyAudio = acmStream.DestBuffer[n] != 0;
    }
    AcmCheck("AcmStream output is not silence", anyAudio, anyAudio ? "non-zero samples" : "all zero");

    // The second conversion reuses the same header block. Before the fix each call started
    // from a tail the previous one had already lost.
    int secondConverted = acmStream.Convert(tone.Length, out _);
    AcmCheck("AcmStream converts repeatedly", secondConverted > 0, $"{secondConverted} bytes");
}
catch (MmException e)
{
    acmFailures++;
    Console.WriteLine($"  FAIL AcmStream threw {e.Result} calling {e.Function}");
}

// Part 2 — the consumer path callers actually reach: Mp3FileReader and AudioFileReader
// both decode through WaveFormatConversionStream.
try
{
    var pcmBytes = GenerateTone(acmSourceFormat, 44100);
    using var rawSource = new RawSourceWaveStream(new MemoryStream(pcmBytes), acmSourceFormat);
    using var conversionStream = new WaveFormatConversionStream(acmTargetFormat, rawSource);
    var readBuffer = new byte[8192];
    int totalRead = 0;
    int read;
    while (totalRead < readBuffer.Length &&
           (read = conversionStream.Read(readBuffer, totalRead, readBuffer.Length - totalRead)) > 0)
    {
        totalRead += read;
    }
    AcmCheck("WaveFormatConversionStream read", totalRead > 0, $"{totalRead} bytes");
}
catch (MmException e)
{
    acmFailures++;
    Console.WriteLine($"  FAIL WaveFormatConversionStream threw {e.Result} calling {e.Function}");
}

// Part 3 — acmFormatSuggest, which takes the WAVEFORMATEX blob rather than the header.
// Fixed in #1427; kept here because GSM 6.10 declares extra data beyond the 18-byte base
// and so exercises the subclass path that rewrite was for.
try
{
    var suggested = AcmStream.SuggestPcmFormat(new Gsm610WaveFormat());
    AcmCheck("SuggestPcmFormat returned PCM", suggested.Encoding == WaveFormatEncoding.Pcm, suggested.Encoding);
    AcmCheck("SuggestPcmFormat kept the sample rate", suggested.SampleRate == 8000, suggested.SampleRate);
}
catch (MmException e)
{
    // The GSM 6.10 codec is optional; a missing one is not a marshalling failure.
    Console.WriteLine($"  SKIP SuggestPcmFormat — acmFormatSuggest returned {e.Result}");
}

Console.WriteLine();
Console.WriteLine(acmFailures == 0
    ? $"  ACM stream header under {runtime}: OK"
    : $"  ACM stream header under {runtime}: FAIL ({acmFailures} check(s))");

return winmmFailures + acmFailures == 0 ? 0 : 1;
