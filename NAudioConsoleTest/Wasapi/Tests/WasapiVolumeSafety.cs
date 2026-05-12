using NAudio.CoreAudioApi;
using Spectre.Console;

namespace NAudioConsoleTest.Wasapi.Tests;

/// <summary>
/// Caps a render endpoint's master volume to a safe level before playback. Used by every
/// WASAPI playback test so a stray test on the user's machine can't blow out their speakers.
/// </summary>
static class WasapiVolumeSafety
{
    public static void CapAt(MMDevice device, float maxScalar = 0.5f)
    {
        try
        {
            var vol = device.AudioEndpointVolume;
            if (vol.MasterVolumeLevelScalar > maxScalar)
            {
                vol.MasterVolumeLevelScalar = maxScalar;
                AnsiConsole.MarkupLine($"[yellow]Volume capped at {maxScalar:P0} for safety[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim]Volume: {vol.MasterVolumeLevelScalar:P0}[/]");
            }
        }
        catch
        {
            // Some endpoints (e.g. virtual devices) don't support endpoint volume — fine to skip.
        }
    }
}
