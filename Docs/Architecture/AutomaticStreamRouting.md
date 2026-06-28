# Automatic Stream Routing for WasapiPlayer / WasapiRecorder

> **Status: IMPLEMENTED.** Tracking issue #942. Surfaced as
> `WasapiPlayerBuilder.WithDefaultDeviceStreamRouting()` +
> `WasapiPlayerBuilder.BuildAsync()` and
> `WasapiRecorderBuilder.WithDefaultDeviceStreamRouting()` (recorder already had `BuildAsync()`),
> backed by `AudioClient.ActivateDefaultDeviceAsync(DataFlow)`. The recommended end-state design
> below (option (b): session volume from the client, standard shared mode only) is what shipped.

## What it is

Since Windows 10 version 1607, a WASAPI client can opt into the same *automatic stream
routing* that the high-level APIs (Media Foundation, DirectSound, WAVE) have had since
Windows 7: when the user changes the default playback/recording device — or unplugs the
current one — the stream is transferred to the new default endpoint seamlessly, with no
application code.

The opt-in is a single change at activation time. Instead of resolving a concrete
`MMDevice` and calling `IMMDevice::Activate`, you call `ActivateAudioInterfaceAsync` with
one of two well-known **device-interface GUIDs** that represent "the current default
device, whatever it is":

| Direction | GUID | Value |
| --- | --- | --- |
| Render  | `DEVINTERFACE_AUDIO_RENDER`  | `{E6327CAD-DCEC-4949-AE8A-991E976A79D2}` |
| Capture | `DEVINTERFACE_AUDIO_CAPTURE` | `{2EEF81BE-33FA-4800-9670-1CD474972C3F}` |

Per the [Automatic Stream Routing][asr] doc, the GUID is turned into a string with
`StringFromIID` and passed as the `deviceInterfacePath`, with `riid = IID_IAudioClient`
and `activationParams = NULL`:

```c
PWSTR path;
StringFromIID(DEVINTERFACE_AUDIO_RENDER, &path);
ActivateAudioInterfaceAsync(path, __uuidof(IAudioClient), NULL, handler, &op);
CoTaskMemFree(path);
```

The returned `IAudioClient` behaves like a normal shared-mode client backed by the current
default endpoint — including `GetMixFormat` (unlike the process-loopback virtual device,
which has no mix format) — and Windows reroutes it under the covers when the default
changes.

[asr]: https://learn.microsoft.com/windows/win32/coreaudio/automatic-stream-routing

## How hard is it? Short answer: **easy plumbing, moderate surface decoupling**

The hard part — the `ActivateAudioInterfaceAsync` + `ComWrappers` + completion-handler
dance — **already exists and is battle-tested** in NAudio. The process-loopback path
(`AudioClient.ActivateProcessLoopbackAsync` → `ActivateAudioInterfaceAsync` →
`ActivateAudioInterfaceCompletionHandler`, all in `NAudio.Wasapi/CoreAudioApi/`) is a
direct template. Stream-routing activation is *strictly simpler* than process loopback: it
passes a fixed device-interface string and **no** `PROPVARIANT`/`AUDIOCLIENT_ACTIVATION_PARAMS`,
so the whole PropVariant-blob marshalling block disappears.

The real work is not the activation — it is that `WasapiPlayer` and `WasapiRecorder` are
both written around the assumption that an `MMDevice` always exists. With routing there is
**no `MMDevice`**, only an `IAudioClient`. That assumption shows up in three places.

### 1. Construction becomes asynchronous

`ActivateAudioInterfaceAsync` is async. `WasapiRecorder` already has the precedent: a
private `WasapiRecorder(AudioClient, …)` constructor plus
`CreateProcessLoopbackAsync`, surfaced through `WasapiRecorderBuilder.BuildAsync()` (with
`Build()` throwing and redirecting). `WasapiPlayer` currently has **only** a synchronous
constructor that eagerly does `device.CreateAudioClient()` and reads `MixFormat`, and the
player builder has no `BuildAsync()` at all — so the player needs:

- a private `WasapiPlayer(AudioClient preActivatedClient, …)` constructor that skips the
  `device.CreateAudioClient()` call, and
- a `WasapiPlayerBuilder.BuildAsync()` mirroring the recorder's pattern.

### 2. The volume APIs assume an endpoint

`WasapiPlayer` exposes endpoint/session volume through `mmDevice`:

- `SessionVolume` → `mmDevice.AudioSessionManager.SimpleAudioVolume`
- `DeviceVolume`  → `mmDevice.AudioEndpointVolume`
- `Volume` / `IsMuted` delegate to `SessionVolume`

With routing, `mmDevice` is `null`. Two options:

- **(a) MVP** — throw `InvalidOperationException` from `DeviceVolume`/`SessionVolume`
  (and therefore `Volume`/`IsMuted`) when routing, with a message pointing at
  `StreamVolume` (which is obtained from the *client*, not the device, so it keeps
  working). Simple and honest, but loses the volume-mixer slider.
- **(b) Better** — obtain `ISimpleAudioVolume` from the client via `GetService` (the
  player already does the analogous thing for `AudioStreamVolume`), so per-session
  volume/mute survive routing. `DeviceVolume` (endpoint-wide) genuinely has no meaning
  for "the default device" and should still throw. This is the recommended end state;
  it is a little extra wiring on `AudioClient`.

`WasapiRecorder` has no device-derived volume surface, so capture routing is unaffected
here.

### 3. The exclusive / low-latency recovery paths re-create the client from the device

Both classes recover from a failed `Initialize`/`InitializeSharedAudioStream` by calling
`mmDevice.CreateAudioClient()` to get a fresh client (e.g.
`WasapiPlayer.TryInitializeLowLatency`, `InitializeWithEventSync`,
`WasapiRecorder.TryInitializeLowLatency`). With no `MMDevice` those paths can't recreate.

This is mostly a non-issue *by design*, because stream routing is inherently a
**shared-mode, follow-the-default** concept:

- **Exclusive mode** is meaningless against a virtual default endpoint (you cannot hold
  exclusive access to something defined as "whichever device is currently default"), so
  routing should force/require shared mode and reject `WithExclusiveMode()`.
- **IAudioClient3 low latency** *could* work (the routing client QIs to `IAudioClient3`),
  but its decline-and-recreate fallback needs a client factory. For the first cut, the
  cleanest choice is to not offer low latency with routing (reject the combination in the
  builder, exactly as the process-loopback path already rejects low latency). A later
  iteration can thread a re-activation delegate through if there's demand.

Restricting routing to **standard shared mode** removes every `mmDevice.CreateAudioClient()`
dependency from the routed path.

### Threading note

Activating an **audio render** device for `IAudioClient` is one of the activations Windows
documents as *explicitly safe* — it does not require the main UI thread — so NAudio's
existing worker-thread model is fine. (Capture and other interfaces are not on that list,
but in practice the same MTA completion-handler path NAudio already uses for process
loopback works; this should be confirmed on a real box.)

## Proposed builder design

Routing is "pick the device" configuration, so it sits naturally alongside `WithDevice`,
`WithLoopbackCapture`, and `WithProcessLoopback`. Proposed name:
`WithDefaultDeviceStreamRouting()` (alternatives considered: `FollowDefaultDevice()`,
`WithAutomaticStreamRouting()`).

### Player

```csharp
// Follow the default render device; re-routes automatically when the default changes.
// Mutually exclusive with WithDevice / WithExclusiveMode / WithLowLatency.
var player = await new WasapiPlayerBuilder()
    .WithDefaultDeviceStreamRouting()
    .BuildAsync();              // async: activation is asynchronous
player.Init(waveProvider);
player.Play();
```

```csharp
public WasapiPlayerBuilder WithDefaultDeviceStreamRouting();   // new
public Task<WasapiPlayer> BuildAsync();                        // new (mirrors recorder)
// Build() throws InvalidOperationException when routing was requested,
// directing the caller to BuildAsync() — exactly as the recorder does for process loopback.
```

### Recorder

```csharp
var recorder = await new WasapiRecorderBuilder()
    .WithDefaultDeviceStreamRouting()   // uses DEVINTERFACE_AUDIO_CAPTURE
    .BuildAsync();
```

```csharp
public WasapiRecorderBuilder WithDefaultDeviceStreamRouting();   // new
// BuildAsync() already exists; add a routing branch next to the process-loopback branch.
```

### Builder validation (both)

`WithDefaultDeviceStreamRouting()` is mutually exclusive with `WithDevice(...)`. The build
methods reject combinations that the routed shared path can't honour, with the same
"call BuildAsync instead" / "not supported with routing" style of message already used for
process loopback:

- Player: reject `WithExclusiveMode`, `WithLowLatency`.
- Recorder: reject `WithExclusiveMode`, `WithLowLatency`, `WithLoopbackCapture`,
  `WithProcessLoopback` (and AEC reference, which needs a concrete endpoint).

## Activation factory (the only new CoreAudio code)

Add to `AudioClient` (clone of `ActivateProcessLoopbackAsync`, minus the activation params):

```csharp
private static readonly Guid DEVINTERFACE_AUDIO_RENDER  = new("E6327CAD-DCEC-4949-AE8A-991E976A79D2");
private static readonly Guid DEVINTERFACE_AUDIO_CAPTURE = new("2EEF81BE-33FA-4800-9670-1CD474972C3F");

/// <summary>
/// Activates an AudioClient that follows the current default endpoint with automatic
/// stream routing (Windows 10 1607+). Requires Windows 10 version 1607 or later.
/// </summary>
public static Task<AudioClient> ActivateDefaultDeviceAsync(DataFlow dataFlow)
{
    var guid = dataFlow == DataFlow.Render ? DEVINTERFACE_AUDIO_RENDER : DEVINTERFACE_AUDIO_CAPTURE;
    // StringFromIID yields the registry-format braced GUID; "B" matches it (case-insensitive path).
    var path = guid.ToString("B").ToUpperInvariant();
    return ActivateAudioInterfaceAsync(path, IID_IAudioClient, IntPtr.Zero, _ => { });
}
```

Notes:

- `ActivateAudioInterfaceAsync` (the existing private helper) already takes a device-path
  string and a null `activationParams`, and the completion handler already wraps the result
  as the base `IAudioClient` and lets the `AudioClient` constructor cross-cast to
  `IAudioClient2/3` — so `WithCategory()` and friends still work where the underlying device
  supports them.
- Using `IID_IAudioClient` (not `IAudioClient2`) keeps render activation on the
  documented *explicitly-safe* path; the cross-cast in the ctor still surfaces v2/v3.
- No `StringFromIID` P/Invoke is needed — `Guid.ToString("B")` produces the same string the
  API expects. (Add the P/Invoke only if a real device rejects the manually-formatted path,
  which would be surprising.)

## Open questions to confirm on real hardware

1. Does `GetMixFormat` / `GetSharedModeEnginePeriod` succeed on the routing client? (Expected
   yes — it is backed by the real default device, unlike process loopback.)
2. Does the routed client QI to `IAudioClient2`/`IAudioClient3` so `WithCategory()` works?
3. Behaviour across an actual default-device switch mid-stream: confirm playback/capture
   continues without a `PlaybackStopped`/`RecordingStopped` and without a manual reinit.
4. Capture activation off the UI thread (render is documented safe; capture is not listed).

## Effort summary

| Piece | Effort |
| --- | --- |
| `ActivateDefaultDeviceAsync` + GUID constants | trivial (clone of existing path) |
| `WasapiPlayer` async ctor + `BuildAsync()` | small |
| `WasapiRecorder` routing branch (private `AudioClient` ctor already exists) | small |
| Builder methods + validation | small |
| Volume decoupling (option b — session volume from client) | small–moderate |
| Tests / docs / release note | small |

**Overall: low-to-moderate.** No new COM marshalling risk; the bulk is decoupling the two
public classes from the "there is always an `MMDevice`" assumption, most of which falls away
once routing is scoped to standard shared mode.
