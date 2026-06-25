using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Alsa;

/// <summary>
/// A PCM device discovered via ALSA's device-name hints.
/// </summary>
public sealed class AlsaDeviceInfo
{
    internal AlsaDeviceInfo(string name, string description)
    {
        Name = name;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// The ALSA PCM name to pass to <see cref="AlsaOut(string)"/> or
    /// <see cref="AlsaIn(string)"/> (e.g. <c>"default"</c>, <c>"hw:CARD=PCH,DEV=0"</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>Human-readable description (may be empty or multi-line).</summary>
    public string Description { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Description})";
}

/// <summary>
/// Enumerates ALSA PCM devices using <c>snd_device_name_hint</c>.
/// </summary>
public static class AlsaDeviceEnumerator
{
    /// <summary>Lists PCM devices that can be opened for playback.</summary>
    public static IReadOnlyList<AlsaDeviceInfo> GetPlaybackDevices() => Enumerate("Output");

    /// <summary>Lists PCM devices that can be opened for capture.</summary>
    public static IReadOnlyList<AlsaDeviceInfo> GetCaptureDevices() => Enumerate("Input");

    private static List<AlsaDeviceInfo> Enumerate(string direction)
    {
        AlsaException.ThrowIfError(
            AlsaInterop.DeviceNameHint(-1, "pcm", out var hints), "snd_device_name_hint");

        var devices = new List<AlsaDeviceInfo>();
        try
        {
            for (int i = 0; ; i++)
            {
                var hint = Marshal.ReadIntPtr(hints, i * IntPtr.Size);
                if (hint == IntPtr.Zero)
                {
                    break;
                }

                var name = AlsaInterop.DeviceNameGetHintString(hint, "NAME");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                // IOID absent => bidirectional; otherwise "Input"/"Output".
                var ioid = AlsaInterop.DeviceNameGetHintString(hint, "IOID");
                if (ioid != null && ioid != direction)
                {
                    continue;
                }

                devices.Add(new AlsaDeviceInfo(
                    name, AlsaInterop.DeviceNameGetHintString(hint, "DESC")));
            }
        }
        finally
        {
            AlsaInterop.DeviceNameFreeHint(hints);
        }

        return devices;
    }
}
