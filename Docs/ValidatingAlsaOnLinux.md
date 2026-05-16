## Validating NAudio.Alsa on Linux (incl. WSL2)

`NAudio.Alsa` is Linux-only and needs a real ALSA stack, so it cannot be
exercised by the Windows CI. This is how to validate it manually. WSL2
with WSLg works for audible playback/capture because WSLg runs a
PulseAudio server and ALSA reaches it through the `pulse` plugin.

### 1. WSL2 prerequisites (one-time)

```powershell
wsl --install -d Ubuntu      # or: wsl --update
wsl --version                # confirm WSL 2.x with WSLg
```

Inside the Ubuntu shell:

```bash
sudo apt update
sudo apt install -y dotnet-sdk-9.0 libasound2 libasound2-plugins alsa-utils
```

`libasound2-plugins` is required — it provides the `pulse` PCM that
routes ALSA to WSLg. Prove WSLg audio works *before* testing NAudio:

```bash
aplay /usr/share/sounds/alsa/Front_Center.wav   # should be audible on Windows
```

If silent: `wsl --update`, check Windows volume, and if `default`
doesn't route, add `~/.asoundrc` with
`pcm.!default { type pulse } ctl.!default { type pulse }`. `pulse` is
the most reliable explicit device name under WSL2.

### 2. Build + headless suite (no audio)

Clone into the Linux filesystem (not `/mnt/c`), then:

```bash
dotnet build NAudio.Alsa/NAudio.Alsa.csproj -c Release
dotnet test --project NAudio.Alsa.Tests/NAudio.Alsa.Tests.csproj -c Release
# expect: 25 passed, 4 skipped (the hardware fixture, gated off)
```

These run against ALSA's `null` PCM and cover interop, lifetime,
the Span data path, enumeration, concurrency and the deadlock
regressions — but produce no sound.

### 3. Audible / real-device validation

The `AlsaHardwareTests` `[IntegrationTest]` fixture self-enables when
`NAUDIO_ALSA_DEVICE` is set (the env-var gate is the driver — no
category-filter syntax needed):

```bash
NAUDIO_ALSA_DEVICE=pulse \
  dotnet test --project NAudio.Alsa.Tests/NAudio.Alsa.Tests.csproj -c Release
```

| Env var | Effect |
|---|---|
| `NAUDIO_ALSA_DEVICE` | playback device; enables the fixture. `pulse`/`default` on WSL2; `hw:0` on real hardware; `null` for a no-audio harness smoke |
| `NAUDIO_ALSA_CAPTURE_DEVICE` | capture device for the round-trip (defaults to `NAUDIO_ALSA_DEVICE`) |
| `NAUDIO_ALSA_WAV` | path to a `.wav`; enables the file-playback test |

Tests: a 1 s 440 Hz tone; a WAV file (if `NAUDIO_ALSA_WAV` set); a
pause/resume (you should hear the gap); a capture→WAV→playback
round-trip. Assertions are "did not throw / completed" — **audible
quality, the pause gap and glitch-free pacing are judged by you**.

For quick interactive trials, a throwaway console using the
`Docs/PlayAudioFileLinuxAlsa.md` snippet is the fastest path.

### What WSL2 covers — and does not

WSL2 routes through Pulse/plug, which resamples/reformats. It validates
audible playback, capture, pause/resume and end-to-end lifecycle. It
does **not** exercise bare `hw:` devices (no `/dev/snd`), the
exact-rate/format negotiation path, real-card xrun/`snd_pcm_recover`
timing, or `snd-aloop` (the default WSL2 kernel lacks the module).
Final sign-off for those needs a real Linux box or a VM with an
emulated sound card.
