## Record an Audio File on Linux with ALSA

The `NAudio.Alsa` package provides `AlsaIn`, an `IWaveIn` that records
from an ALSA capture device. Add the package (the runtime needs
`libasound` — `sudo apt install libasound2`):

```sh
dotnet add package NAudio.Alsa
```

Recording follows the standard NAudio pattern: set the `WaveFormat`,
subscribe to `DataAvailable` and write the captured bytes to a
`WaveFileWriter`, then `StartRecording` / `StopRecording`. `AlsaIn`
raises `RecordingStopped` once the capture thread has finished, which is
the safe point to dispose the writer.

```c#
using NAudio.Wave;
using NAudio.Wave.Alsa;

var waveFormat = new WaveFormat(44100, 16, 2);
var writer = new WaveFileWriter("recorded.wav", waveFormat);

using var input = new AlsaIn();           // or new AlsaIn("hw:0")
input.WaveFormat = waveFormat;

input.DataAvailable += (sender, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
};

input.RecordingStopped += (sender, a) =>
{
    writer.Dispose();
    writer = null;
};

input.StartRecording();
Thread.Sleep(5000);                       // record for 5 seconds
input.StopRecording();
```

Pick a specific capture device by passing an ALSA PCM name to the
constructor, or enumerate them:

```c#
foreach (var device in AlsaDeviceEnumerator.GetCaptureDevices())
{
    Console.WriteLine($"{device.Name} - {device.Description}");
}
// using var input = new AlsaIn("hw:1");
```

The `WaveInEventArgs` buffer is reused on the next callback, so write or
copy it before the handler returns. The recording format must be one
the device supports (8/16/24/32-bit PCM or 32-bit IEEE float); an
unsupported format throws an `AlsaException`.
