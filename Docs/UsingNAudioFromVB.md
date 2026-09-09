# Using NAudio from VB.NET

NAudio 3 uses `Span<T>` and `ReadOnlySpan<T>` throughout its buffer APIs. The VB.NET compiler
does not support "byref-like" types (ref structs), which raises the obvious question of how much
of NAudio is still reachable from VB.

The short answer: **almost all of it**. VB can *call* span-based APIs with no ceremony at all —
you can pass an ordinary array where a `Span(Of T)` is expected. The one thing VB cannot do is
*write* the type name `Span(Of T)` in your own source, which means you cannot directly implement
`ISampleProvider` or `IWaveProvider`. NAudio provides array-based base classes for that case.

All the examples below compile with `Option Strict On`.

## What works, and what doesn't

The single error you will hit is:

```
BC30668: 'Span(Of Byte)' is obsolete: 'Types with embedded references are not supported
in this version of your compiler.'
```

VB raises it whenever your source *names* a ref struct. That distinction is the whole story:

| What you want to do | VB |
| --- | --- |
| Pass an array to a `Span<T>` parameter — `src.Read(buffer)` | ✔ works, no conversion needed |
| Pass a slice — `src.Read(buffer.AsSpan(0, 512))` | ✔ |
| Call `.Length`, `.ToArray()`, `.Fill()`, `.Clear()`, `.CopyTo()` on a span | ✔ |
| `For Each` over a span | ✔ |
| Pass a span straight from one NAudio API to another | ✔ |
| Declare a span variable — `Dim s As Span(Of Byte)` | ✘ BC30668 |
| Implement or override a method taking a span | ✘ BC30668 — use the base classes below |
| Index a span — `buffer(0)` or `buffer(0) = x` | ✘ BC30643 / BC30068 — call `.ToArray()` first |
| Call a method with an `out Span(Of T)` parameter, e.g. `WdlResampler.ResamplePrepare` | ✘ needs a local |
| `Await For Each` over `WasapiRecorder.CaptureAsync` | ✘ VB has no async-stream syntax |

## Playing audio

Playback needs no workarounds at all — VB only ever calls into NAudio here:

```vb
Imports NAudio.Wave

Dim reader As New AudioFileReader("test.mp3")
Dim player = New WasapiPlayerBuilder().Build()
player.Init(reader)
player.Play()
```

`WaveOut`, `WasapiPlayer`, `AsioOut`, every built-in `ISampleProvider`
(`MixingSampleProvider`, `VolumeSampleProvider`, `FadeInOutSampleProvider`, `SignalGenerator`,
`OffsetSampleProvider`, …) and the whole of `NAudio.Dsp` are usable exactly as they are from C#.

Note that you do **not** need `AsSpan` when calling a `Read` method. VB applies the array-to-span
conversion for you, so both of these compile:

```vb
Dim buffer(1023) As Single
Dim samplesRead = provider.Read(buffer)                  ' whole buffer
Dim partialRead = provider.Read(buffer.AsSpan(0, 512))  ' first 512 samples
```

## Recording audio

### WaveIn and the legacy WASAPI capture classes

`WaveIn`, `WasapiCapture` and `WasapiLoopbackCapture` all expose
`DataAvailable As EventHandler(Of WaveInEventArgs)`, and `WaveInEventArgs.Buffer` is a plain
`Byte()`. Nothing special is required:

```vb
Private recorder As WaveIn
Private writer As WaveFileWriter

Public Sub StartRecording(path As String)
    recorder = New WaveIn()
    recorder.WaveFormat = New WaveFormat(44100, 16, 1)
    writer = New WaveFileWriter(path, recorder.WaveFormat)
    AddHandler recorder.DataAvailable, AddressOf OnDataAvailable
    recorder.StartRecording()
End Sub

Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
    writer.Write(e.Buffer, 0, e.BytesRecorded)
End Sub
```

### WasapiRecorder

`WasapiRecorder.DataAvailable` uses a delegate whose first parameter is a `ReadOnlySpan(Of Byte)`:

```c#
public delegate void CaptureDataAvailableHandler(ReadOnlySpan<byte> buffer,
    AudioClientBufferFlags flags, long devicePosition, long qpcPosition);
```

You cannot write a named handler for it, because `AddressOf` requires you to spell out the
parameter types. But **a lambda with inferred parameter types works**, because your source never
names the span type:

```vb
Imports NAudio.Wave

Private writer As WaveFileWriter

Public Sub StartRecording(path As String)
    Dim recorder = New WasapiRecorderBuilder().Build()
    writer = New WaveFileWriter(path, recorder.WaveFormat)

    AddHandler recorder.DataAvailable,
        Sub(buffer, flags, devicePosition, qpcPosition)
            writer.Write(buffer)
        End Sub

    AddHandler recorder.RecordingStopped,
        Sub(sender, e)
            writer.Dispose()
        End Sub

    recorder.StartRecording()
End Sub
```

Inside that lambda you can:

- read `buffer.Length`
- call `buffer.ToArray()` to get a `Byte()` copy
- forward `buffer` to any other span-taking API — `WaveFileWriter.Write`,
  `BufferedWaveProvider.AddSamples`, and so on

You **cannot** index `buffer(i)`. If you need per-sample access, copy first:

```vb
AddHandler recorder.DataAvailable,
    Sub(buffer, flags, devicePosition, qpcPosition)
        Dim bytes = buffer.ToArray()
        ' work with bytes() as normal
    End Sub
```

That costs one copy per packet, which is what you would pay in NAudio 2 anyway.

If you need to unsubscribe later, store the lambda first — the delegate type itself is not a ref
struct, so VB is happy to name it:

```vb
Private handler As CaptureDataAvailableHandler

handler = Sub(buffer, flags, devicePosition, qpcPosition)
              writer.Write(buffer)
          End Sub
AddHandler recorder.DataAvailable, handler
' ...
RemoveHandler recorder.DataAvailable, handler
```

### CaptureAsync

`WasapiRecorder.CaptureAsync` returns an `IAsyncEnumerable(Of AudioBuffer)`. VB has no
equivalent of C#'s `await foreach`, so you must drive the enumerator yourself. Note that `Await`
is not allowed inside a `Finally` block in VB, so dispose after the loop:

```vb
Public Async Function RecordAsync(token As CancellationToken) As Task
    Dim recorder = New WasapiRecorderBuilder().Build()
    Dim enumerator = recorder.CaptureAsync(token).GetAsyncEnumerator(token)
    While Await enumerator.MoveNextAsync()
        Dim bytes = enumerator.Current.Data.ToArray()
        ' process bytes()
    End While
    Await enumerator.DisposeAsync()
End Function
```

`AudioBuffer.Data` is a `ReadOnlyMemory(Of Byte)`, not a span, so it is fully usable from VB.
For most VB applications the `DataAvailable` lambda above is simpler.

## Writing your own providers

This is the one place where VB genuinely cannot use the standard interfaces. `ISampleProvider`
and `IWaveProvider` both declare their `Read` method in terms of a span, and implementing them
would mean naming that type.

NAudio provides two base classes for this, with the array-based `Read` signature NAudio 2 used.
Derive from these and override `Read(buffer(), offset, count)`:

### SampleProviderBase — for 32 bit float providers

`SampleProviderBase` implements both `ISampleProvider` and `IWaveProvider`, so a derived class can
go straight into a mixer or into `player.Init(...)`. It is the array-based equivalent of
`WaveProvider32`:

```vb
Imports NAudio.Wave

Public Class SineProvider
    Inherits SampleProviderBase

    Private phase As Double
    Public Property Frequency As Double = 440
    Public Property Amplitude As Single = 0.25F

    Public Sub New(sampleRate As Integer, channels As Integer)
        MyBase.New(sampleRate, channels)
    End Sub

    Public Overrides Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer
        For i = 0 To count - 1
            phase += 2 * Math.PI * Frequency / WaveFormat.SampleRate
            buffer(offset + i) = CSng(Math.Sin(phase)) * Amplitude
        Next
        Return count
    End Function
End Class
```

It then composes with the rest of NAudio as normal:

```vb
Dim sine As New SineProvider(44100, 1)
Dim volume As New VolumeSampleProvider(sine) With {.Volume = 0.5F}
Dim player = New WasapiPlayerBuilder().Build()
player.Init(volume)
player.Play()
```

### WaveProviderBase — for providers of any format

If your provider produces something other than 32 bit float — 16 bit PCM, say — derive from
`WaveProviderBase` and pass the format to the base constructor:

```vb
Imports NAudio.Wave

Public Class SilenceProvider
    Inherits WaveProviderBase

    Public Sub New()
        MyBase.New(New WaveFormat(44100, 16, 2))
    End Sub

    Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
        Array.Clear(buffer, offset, count)
        Return count
    End Function
End Class
```

Both base classes bridge to the span-based interface by renting a buffer from
`ArrayPool(Of T).Shared` and copying once per read. That cost is negligible next to the audio
work itself, but it is why C# code should implement `ISampleProvider` directly instead.

### Deriving from WaveStream

If you need seeking and a length as well, derive from `WaveStream`. It inherits from
`System.IO.Stream`, whose `Read(Byte(), Integer, Integer)` overload is overridable without naming
a span — the base class bridges to `Read(Span(Of Byte))` for you:

```vb
Public Class MyWaveStream
    Inherits WaveStream

    Private ReadOnly fmt As New WaveFormat(44100, 16, 2)

    Public Overrides ReadOnly Property WaveFormat As WaveFormat
        Get
            Return fmt
        End Get
    End Property

    Public Overrides ReadOnly Property Length As Long
        Get
            Return 0
        End Get
    End Property

    Public Overrides Property Position As Long

    Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
        ' fill buffer
        Return count
    End Function
End Class
```

## What is still out of reach

A small number of extension points require a span in a signature you would have to write, and
have no array-based equivalent. From VB you cannot:

- implement `IAudioEffect` or derive from `AudioEffect` (custom effects for `NAudio.Effects`)
- implement `IMp3FrameDecompressor` (custom MP3 decoders)
- derive from `WaveProvider32`, `WaveProvider16` or `SampleProviderConverterBase`
  (use `SampleProviderBase` / `WaveProviderBase` instead)
- supply the ASIO `AsioFloatToNativeConverter.ConverterFn` /
  `AsioNativeToFloatConverter.ConverterFn` delegates
- call `WdlResampler.ResamplePrepare`, which takes an `out Span(Of Single)`

For these, the practical answer is a small C# class library in the same solution that implements
the interface and exposes a VB-friendly surface — the same approach you would take for any
C#-only language feature.

## See also

- [Migrating from NAudio 2 to NAudio 3](MigratingFromNAudio2.md)
- [WaveStream, IWaveProvider and ISampleProvider](WaveProviders.md)
- [Recording with WasapiRecorder](WasapiRecorder.md)
