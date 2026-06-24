using NAudioConsoleTest.Asio.Tests;
using NAudioConsoleTest.DirectSound.Tests;
using NAudioConsoleTest.Dmo.Tests;
using NAudioConsoleTest.Dsp.Tests;
using NAudioConsoleTest.MediaFoundation.Tests;
using NAudioConsoleTest.SoundFile.Tests;
using NAudioConsoleTest.Vst3.Tests;
using NAudioConsoleTest.Wasapi.Tests;
using NAudioConsoleTest.WinMM.Tests;

namespace NAudioConsoleTest.Shared.Testing;

/// <summary>
/// Single hand-maintained list of every test registered with <see cref="TestRegistry"/>.
/// Add new <see cref="IConsoleTest"/> implementations here as they're written.
/// </summary>
static class TestRegistration
{
    public static void RegisterAll()
    {
        TestRegistry.Register(new AsioListDriversTest());
        TestRegistry.Register(new AsioShowCapabilitiesTest());
        TestRegistry.Register(new AsioPlayAudioFileTest());
        TestRegistry.Register(new AsioPlayShortTestToneTest());
        TestRegistry.Register(new AsioRecordToWavTest());
        TestRegistry.Register(new AsioShowChannelLevelsTest());
        TestRegistry.Register(new AsioDuplexPassthroughTest());
        TestRegistry.Register(new AsioLegacyDuplexPassthroughTest());
        TestRegistry.Register(new AsioReinitializeRoundTripTest());
        TestRegistry.Register(new AsioValidateSamplePositionTest());
        TestRegistry.Register(new AsioDisposeFromStoppedHandlerTest());
        TestRegistry.Register(new AsioStopFromCallbackGuardTest());

        TestRegistry.Register(new MediaFoundationReadAudioFileTest());
        TestRegistry.Register(new MediaFoundationReadImmediateRepositionTest());
        TestRegistry.Register(new MediaFoundationReadFromStreamTest());
        TestRegistry.Register(new MediaFoundationEncodeToMp3Test());
        TestRegistry.Register(new MediaFoundationEncodeToAacTest());
        TestRegistry.Register(new MediaFoundationEncodeToWmaTest());
        TestRegistry.Register(new MediaFoundationEncodeToFlacTest());
        TestRegistry.Register(new MediaFoundationRoundTripTest());
        TestRegistry.Register(new MediaFoundationResampleFileTest());
        TestRegistry.Register(new MediaFoundationEnumerateTransformsTest());

        TestRegistry.Register(new SoundFileShowCapabilitiesTest());
        TestRegistry.Register(new SoundFilePlayFileTest());
        TestRegistry.Register(new SoundFileTranscodeTest());
        TestRegistry.Register(new SoundFileRoundTripTest());
        TestRegistry.Register(new SoundFileStreamRoundTripTest());

        TestRegistry.Register(new WasapiListDevicesTest());
        TestRegistry.Register(new WasapiExclusiveQuickScanTest());
        TestRegistry.Register(new WasapiExclusiveDetailedScanTest());
        TestRegistry.Register(new WasapiExclusiveChannelMaskDeepDiveTest());
        TestRegistry.Register(new WasapiFindBestExclusiveFormatTest());
        TestRegistry.Register(new WasapiPlayFileTest());
        TestRegistry.Register(new WasapiPlaySineWaveTest());
        TestRegistry.Register(new WasapiRecordAndPlaybackTest());
        TestRegistry.Register(new WasapiRecordToWavFileTest());
        TestRegistry.Register(new WasapiDeviceNotificationWatcherTest());
        TestRegistry.Register(new WasapiVolumeCallbackStressTest());

        TestRegistry.Register(new DspWdlResampleFileTest());

        TestRegistry.Register(new DirectSoundListDevicesTest());
        TestRegistry.Register(new DirectSoundPlayToneTest());

        TestRegistry.Register(new DmoResampleFileTest());
        TestRegistry.Register(new DmoDecodeMp3Test());
        TestRegistry.Register(new DmoEchoEffectTest());

        TestRegistry.Register(new WinMmDecodeMp3Test());
        TestRegistry.Register(new WinMmConvertWithProviderTest());
        TestRegistry.Register(new WinMmConvertWithStreamTest());
        TestRegistry.Register(new WinMmPlayFileTest());
        TestRegistry.Register(new WinMmRecordToFileTest());

        TestRegistry.Register(new Vst3ListPluginsTest());
        TestRegistry.Register(new Vst3RenderEffectTest());
        TestRegistry.Register(new Vst3RenderInstrumentTest());
        TestRegistry.Register(new Vst3RenderMidiFileTest());
        TestRegistry.Register(new Vst3LiveSynthTest());
        TestRegistry.Register(new Vst3ParamSweepTest());
        TestRegistry.Register(new Vst3StateRoundtripTest());
        TestRegistry.Register(new Vst3SelfRoundtripTest());
        TestRegistry.Register(new Vst3ShowEditorTest());
        TestRegistry.Register(new Vst3ListProgramsTest());
    }
}
