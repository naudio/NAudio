using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace NAudio.Midi;

/// <summary>
/// Represents a MIDI out device backed by the legacy winmm <c>midiOut*</c> API.
/// </summary>
public class MidiOut : IMidiOutput
{
    /// <summary>Fixed part of the time we allow a driver to release a long-message buffer.</summary>
    private const int UnprepareBaseTimeoutMilliseconds = 5000;

    /// <summary>
    /// Extra time allowed per byte of a long message. A DIN connection runs at 31250 baud, or
    /// about 0.32 ms per byte, so this leaves roughly threefold headroom - long enough that a
    /// genuine sysex dump is never cut short, short enough to bound a wedged driver.
    /// </summary>
    private const int UnprepareMillisecondsPerByte = 1;

    /// <summary>Ceiling on the backoff between <c>midiOutUnprepareHeader</c> retries.</summary>
    private const int MaxUnprepareDelayMilliseconds = 50;

    private readonly IntPtr hMidiOut = IntPtr.Zero;
    private bool disposed = false;
    private readonly MidiInterop.MidiOutCallback callback;

    /// <summary>
    /// Gets the number of MIDI devices available in the system
    /// </summary>
    public static int NumberOfDevices
    {
        get
        {
            return MidiInterop.midiOutGetNumDevs();
        }
    }

    /// <summary>
    /// Gets the MIDI Out device info
    /// </summary>
    public static MidiOutCapabilities DeviceInfo(int midiOutDeviceNumber)
    {
        MidiOutCapabilities caps = new MidiOutCapabilities();
        int structSize = Marshal.SizeOf(caps);
        MmException.Try(MidiInterop.midiOutGetDevCaps(midiOutDeviceNumber, out caps, structSize), "midiOutGetDevCaps");
        return caps;
    }


    /// <summary>
    /// Opens a specified MIDI out device
    /// </summary>
    /// <param name="deviceNo">The device number</param>
    public MidiOut(int deviceNo)
    {
        this.callback = new MidiInterop.MidiOutCallback(Callback);
        MmException.Try(MidiInterop.midiOutOpen(out hMidiOut, deviceNo, callback, IntPtr.Zero, MidiInterop.CALLBACK_FUNCTION), "midiOutOpen");
    }

    /// <summary>
    /// Closes this MIDI out device
    /// </summary>
    public void Close()
    {
        Dispose();
    }

    /// <summary>
    /// Closes this MIDI out device
    /// </summary>
    public void Dispose()
    {
        GC.KeepAlive(callback);
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets or sets the volume for this MIDI out device
    /// </summary>
    public int Volume
    {
        // TODO: Volume can be accessed by device ID
        get
        {
            int volume = 0;
            MmException.Try(MidiInterop.midiOutGetVolume(hMidiOut, ref volume), "midiOutGetVolume");
            return volume;
        }
        set
        {
            MmException.Try(MidiInterop.midiOutSetVolume(hMidiOut, value), "midiOutSetVolume");
        }
    }

    /// <summary>
    /// Resets the MIDI out device
    /// </summary>
    public void Reset()
    {
        MmException.Try(MidiInterop.midiOutReset(hMidiOut), "midiOutReset");
    }

    /// <summary>
    /// Sends a MIDI out message
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="param1">Parameter 1</param>
    /// <param name="param2">Parameter 2</param>
    public void SendDriverMessage(int message, int param1, int param2)
    {
        MmException.Try(MidiInterop.midiOutMessage(hMidiOut, message, param1, param2), "midiOutMessage");
    }

    /// <summary>
    /// Sends a MIDI message to the MIDI out device
    /// </summary>
    /// <param name="message">The message to send</param>
    public void Send(int message)
    {
        MmException.Try(MidiInterop.midiOutShortMsg(hMidiOut, message), "midiOutShortMsg");
    }

    /// <summary>
    /// Closes the MIDI out device
    /// </summary>
    /// <param name="disposing">True if called from Dispose</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            // The constructor throws if midiOutOpen fails, but the finalizer still runs on the
            // half-constructed object, so there may be no handle to close.
            if (hMidiOut != IntPtr.Zero)
            {
                MidiInterop.midiOutClose(hMidiOut);
            }
        }
        disposed = true;
    }

    private void Callback(IntPtr midiInHandle, MidiInterop.MidiOutMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2)
    {
    }

    /// <summary>
    /// Send a long message, for example sysex.
    /// </summary>
    /// <remarks>
    /// Blocks until the driver has finished with the buffer. For a large sysex dump over a
    /// 31250 baud DIN connection that can take a noticeable amount of time, so call this from a
    /// worker thread rather than a UI thread.
    /// </remarks>
    /// <param name="byteBuffer">The bytes to send.</param>
    public void SendBuffer(byte[] byteBuffer)
    {
        if (byteBuffer == null) throw new ArgumentNullException(nameof(byteBuffer));

        var headerSize = Marshal.SizeOf<MidiInterop.MIDIHDR>();
        var lpData = IntPtr.Zero;
        var lpHeader = IntPtr.Zero;
        var prepared = false;
        var driverOwnsBuffer = false;
        try
        {
            lpData = Marshal.AllocHGlobal(byteBuffer.Length);
            lpHeader = Marshal.AllocHGlobal(headerSize);
            Marshal.Copy(byteBuffer, 0, lpData, byteBuffer.Length);

            var header = new MidiInterop.MIDIHDR
            {
                lpData = lpData,
                dwBufferLength = byteBuffer.Length,
                dwBytesRecorded = byteBuffer.Length,
            };
            Marshal.StructureToPtr(header, lpHeader, false);

            MmException.Try(MidiInterop.midiOutPrepareHeader(hMidiOut, lpHeader, headerSize), "midiOutPrepareHeader");
            prepared = true;

            // From here on the driver may own the buffer even if the send fails, so we can only
            // free once midiOutUnprepareHeader tells us it has handed it back.
            driverOwnsBuffer = true;
            MmException.Try(MidiInterop.midiOutLongMsg(hMidiOut, lpHeader, headerSize), "midiOutLongMsg");
        }
        finally
        {
            if (prepared)
            {
                driverOwnsBuffer = !TryUnprepareHeader(lpHeader, headerSize, byteBuffer.Length);
            }

            // Leaking is the lesser evil: freeing a buffer the driver still holds corrupts the
            // message being transmitted, or the heap.
            if (!driverOwnsBuffer)
            {
                if (lpHeader != IntPtr.Zero) Marshal.FreeHGlobal(lpHeader);
                if (lpData != IntPtr.Zero) Marshal.FreeHGlobal(lpData);
            }
        }
    }

    /// <summary>
    /// Unprepares a long-message header, waiting for the driver to release the buffer first.
    /// </summary>
    /// <returns>True if the header was unprepared and its buffers can safely be freed.</returns>
    private bool TryUnprepareHeader(IntPtr lpHeader, int headerSize, int bufferLength)
    {
        // The driver may still own the buffer after midiOutLongMsg returns - that is what
        // MHDR_INQUEUE, MOM_DONE and MIDIERR_STILLPLAYING exist to express - so poll rather than
        // assuming the send completed synchronously. Back off instead of spinning.
        var result = MidiInterop.midiOutUnprepareHeader(hMidiOut, lpHeader, headerSize);
        var timeout = UnprepareBaseTimeoutMilliseconds + (long)bufferLength * UnprepareMillisecondsPerByte;
        var deadline = Environment.TickCount64 + timeout;
        var delayMilliseconds = 1;
        while (result == MmResult.MidiStillPlaying && Environment.TickCount64 < deadline)
        {
            Thread.Sleep(delayMilliseconds);
            delayMilliseconds = Math.Min(delayMilliseconds * 2, MaxUnprepareDelayMilliseconds);
            result = MidiInterop.midiOutUnprepareHeader(hMidiOut, lpHeader, headerSize);
        }

        if (result == MmResult.MidiStillPlaying)
        {
            // midiOutReset marks every pending buffer as done, so this is the last way to get the
            // buffer back from a driver that has stopped making progress.
            MidiInterop.midiOutReset(hMidiOut);
            result = MidiInterop.midiOutUnprepareHeader(hMidiOut, lpHeader, headerSize);
        }

        return result == MmResult.NoError;
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    ~MidiOut()
    {
        System.Diagnostics.Debug.Assert(hMidiOut == IntPtr.Zero, "MIDI Out was not finalised");
        Dispose(false);
    }
}
