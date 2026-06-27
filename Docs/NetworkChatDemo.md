# Streaming live audio over the network (Network Chat demo)

The **Network Chat** panel in `NAudioDemo` is a small two-way voice-chat sample. It captures
your microphone, encodes it with a codec of your choice, sends it over the network, and plays
back whatever it receives from a peer. It is a good starting point if you want to build any kind
of live audio streaming on top of NAudio.

This page explains how to run it (on one machine or across two) and how the pieces fit together.

## Running it

Launch `NAudioDemo`, choose **Network Chat** from the demo list, then fill in:

| Field | Meaning |
| --- | --- |
| **Remote host** | The machine to send your audio to – an IP address (`192.168.1.50`) or a host name. |
| **Remote port** | The UDP port the remote machine is listening on. |
| **Listen port** | The port *this* instance listens on for incoming audio. |
| **Input device** | The microphone to capture from. |
| **Codec** | How the audio is compressed before sending (see below). |

Press **Start Streaming** to begin and **Stop** to end. Audio is sent over UDP (see
[Why UDP](#why-udp) below).

### Two machines

Run the demo on both PCs. On each one, set **Remote host** to the *other* machine and use the
**same port** for remote and listen on both sides:

* PC A (`192.168.1.10`): Remote host `192.168.1.20`, Remote port `7080`, Listen port `7080`
* PC B (`192.168.1.20`): Remote host `192.168.1.10`, Remote port `7080`, Listen port `7080`

Make sure any firewall allows the chosen UDP port. Because there is no connection to establish,
the two ends can be started in any order.

### One machine (two instances)

You can experiment with just one PC by running two copies of the demo and **swapping the ports**
so each instance listens on the port the other sends to:

* Instance A: Remote host `127.0.0.1`, Remote port `7081`, Listen port `7080`
* Instance B: Remote host `127.0.0.1`, Remote port `7080`, Listen port `7081`

(If you point **Remote host** straight back at yourself with matching ports, you simply hear your
own voice echoed back – handy for a quick loopback test.)

> **Note:** because this is full-duplex and plays out of your speakers, using it on one machine
> with a live microphone can produce acoustic feedback. Use headphones, or a virtual input.

## Codecs

The demo ships several codecs, discovered automatically by reflection (each implements
`INetworkChatCodec`). They fall into two groups:

* **Opus** (narrow / wide / full band) – a modern, royalty-free codec implemented in pure managed
  code via [Concentus](https://github.com/lostromb/concentus), so it works on every platform.
  Opus gives far better quality per kilobit than the old telephony codecs and is the default.
  **Wide-band Opus (16 kHz)** is a good general-purpose choice for voice.
* **Legacy telephony codecs** – G.711 a-law/µ-law, GSM 6.10, G.722, Microsoft ADPCM, DSP Group
  TrueSpeech and uncompressed PCM. These are kept mainly to demonstrate NAudio's `AcmStream` and
  built-in codec support. The ACM-based ones are Windows-only and are hidden automatically if the
  codec is not installed (`INetworkChatCodec.IsAvailable`).

For anything new, prefer Opus.

## How it works

The demo separates capture/playback from transport so you can reuse either piece:

```
microphone ─▶ WasapiRecorder ─▶ codec.Encode ─▶ IAudioSender ──network──▶ IAudioReceiver ─▶ codec.Decode ─▶ BufferedWaveProvider ─▶ WasapiPlayer ─▶ speakers
```

* **`NetworkAudioSender`** captures with [`WasapiRecorder`](WasapiRecorder.md) (NAudio 3's
  recommended capture device), encodes each ~50 ms buffer and hands the bytes to an `IAudioSender`.
  WASAPI shared mode converts the device mix format to the codec's record format, so any codec
  sample rate works without manual resampling.
* **`NetworkAudioPlayer`** receives encoded bytes from an `IAudioReceiver`, decodes them, and feeds
  a `BufferedWaveProvider` (acting as a small jitter buffer) that a [`WasapiPlayer`](WasapiPlayer.md)
  plays. The buffer is capped at ~500 ms with `DiscardOnBufferOverflow = true`, so latency stays
  bounded if packets arrive in bursts.
* **Transport** is behind `IAudioSender` / `IAudioReceiver` (a seam you can swap out):
  `UdpAudioSender` / `UdpAudioReceiver` send one datagram per encoded chunk.

The receiver binds to `IPAddress.Any`, so audio from other machines is received – not just
loopback traffic. (An earlier version of this demo bound to `IPAddress.Loopback`, which is why it
only ever worked between two instances on the same PC.)

### Why UDP

This demo streams over UDP only, because UDP is the right transport for real-time audio:

* A lost or late datagram produces a brief glitch; playback simply carries on with the next packet.
* There is no connection to establish, so peers can start in any order.

TCP, by contrast, guarantees delivery and ordering – which sounds appealing but is the wrong
trade-off here. A single lost segment causes *head-of-line blocking*: every packet behind it waits
for a retransmit, adding latency that keeps growing and never recovers. That is why real voice/video
systems build on UDP (RTP, WebRTC, SRT, QUIC) rather than raw TCP. If you genuinely need to traverse
a network that blocks UDP, reach for one of those protocols rather than streaming audio down a TCP
socket.

## Building your own

To stream audio in your own app, the smallest version of this is:

1. Capture with `WasapiRecorder` (via `WasapiRecorderBuilder`) and subscribe to `DataAvailable`.
2. Optionally encode with a codec (Opus via Concentus is a great default).
3. Send the bytes over a `UdpClient`.
4. On the receiving side, decode and push into a `BufferedWaveProvider` with
   `DiscardOnBufferOverflow = true`, played by a `WasapiPlayer`.

The `NetworkChatDemo` source under `samples/NAudioDemo/NetworkChatDemo` is a complete, working
reference for each of those steps.
