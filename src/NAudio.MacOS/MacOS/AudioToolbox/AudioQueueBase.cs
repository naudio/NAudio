
using System;
using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides the base scaffolding code for implementing the Audio Queue API. <br />
/// Not meant to be extended by external code. <br />
/// Instead, use the dedicated subclasses of this class, 
/// located at the <see cref="NAudio.Wave"/> namespace.
/// </summary>
public abstract unsafe class AudioQueueBase : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Provides the handle to the underlying Audio Queue object. <br />
    /// This should be assigned to a valid value by the derived objects.
    /// </summary>
    protected IntPtr queueHandle;

    private AudioQueueLevelMeterData? data;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioQueueBase"/> class.
    /// </summary>
    public AudioQueueBase()
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        data = null;
    }

    /// <summary>
    /// Gets level metering data for this audio queue.
    /// </summary>
    /// <remarks>
    /// Do not cache the value of this property to a field or to a variable:
    /// the level meter data are updated from repeated calls of this property.
    /// </remarks>
    public AudioQueueLevelMeterData LevelMeter
    {
        get
        {
            if (data is null)
            {
                data = new();
                if (queueHandle == IntPtr.Zero) { return data; }
                SetUIntProperty(AudioQueueProperties.kAudioQueueProperty_EnableLevelMetering, 1U);
            }
            if (queueHandle == IntPtr.Zero) { return data; }
            try
            {
                // For now, let's not relay any exceptions to the caller
                // TODO: Verify in which states this does throw, 
                // and if does throw, what it does throw.
                data.UpdateMeterData(GetAudioLevelMeterStates());
            }
            catch { }
            return data;
        }
    }

    private AudioQueueLevelMeterState[] GetAudioLevelMeterStates()
    {
        AudioQueueLevelMeterState[] states;
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueGetPropertySize(
                queueHandle,
                AudioQueueProperties.kAudioQueueProperty_CurrentLevelMeter,
                out var size
            )
        );
        states = new AudioQueueLevelMeterState[size / sizeof(AudioQueueLevelMeterState)];
        fixed (AudioQueueLevelMeterState* ptrStates = states)
        {
            AudioQueueException.ThrowIfError(
                NativeMethods.AudioQueueGetProperty(
                    queueHandle,
                    AudioQueueProperties.kAudioQueueProperty_CurrentLevelMeter,
                    new(ptrStates),
                    ref size
                )
            );
        }
        Array.Resize(ref states, (int)(size / sizeof(AudioQueueLevelMeterState)));
        return states;
    }

    #region Properties, listeners and parameters

    /// <summary>
    /// Queries the value of a specific Audio Queue parameter.
    /// </summary>
    /// <param name="id">The ID of the parameter to query.</param>
    /// <returns>The current value of the parameter.</returns>
    internal float QueryParameter(AudioQueueParameterID id)
    {
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueGetParameter(queueHandle, id, out var value)
        );
        return value;
    }

    /// <summary>
    /// Updates the value of a specific Audio Queue parameter.
    /// </summary>
    /// <param name="id">The ID of the parameter to update.</param>
    /// <param name="newValue">The new value of the parameter.</param>
    internal void SetParameter(AudioQueueParameterID id, float newValue)
    {
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueSetParameter(queueHandle, id, newValue)
        );
    }

    internal uint GetUIntProperty(AudioQueuePropertyID property)
    {
        uint value;
        uint size = sizeof(uint);
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueGetProperty(
                queueHandle,
                property,
                new(&value),
                ref size
            )
        );
        return value;
    }

    internal string GetStringProperty(AudioQueuePropertyID property)
    {
        nint stringNative;
        uint size = (uint)IntPtr.Size;
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueGetProperty(
                queueHandle,
                property,
                new(&stringNative),
                ref size
            )
        );
        using var strInterop = new CoreFoundationApi.CFString(stringNative);
        return strInterop.ToString();
    }

    internal ChannelLayoutHandle? GetChannelLayout()
    {
        uint size;
        int status = NativeMethods.AudioQueueGetPropertySize(
            queueHandle,
            AudioQueueProperties.kAudioQueueProperty_ChannelLayout,
            out size
        );
        if (status != 0) { return null; }
        var handle = ChannelLayoutHandle.Allocate(size);
        status = NativeMethods.AudioQueueGetProperty(
            queueHandle,
            AudioQueueProperties.kAudioQueueProperty_ChannelLayout,
            handle.DangerousGetHandle(),
            ref size
        );
        if (status == 0)
        {
            return handle;
        }
        else
        {
            handle.Dispose();
            return null;
        }
    }

    internal void SetUIntProperty(AudioQueuePropertyID property, uint value)
        => AudioQueueException.ThrowIfError(NativeMethods.AudioQueueSetProperty(queueHandle, property, new(&value), sizeof(uint)));

    internal void SetStringProperty(AudioQueuePropertyID property, string value)
    {
        using var nativeString = CoreFoundationApi.CFString.CreateFromString(value);
        nint nativeValue = nativeString.DangerousGetObject();
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueSetProperty(
                queueHandle,
                property,
                new(&nativeValue),
                (uint)IntPtr.Size
            )
        );
    }

    internal void SetChannelLayoutProperty(AudioQueuePropertyID property, AudioChannelLayout* layout)
        => AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueSetProperty(
                queueHandle,
                property,
                new(layout),
                (uint)IntPtr.Size
            )
        );

    internal void SetChannelLayoutProperty(AudioQueuePropertyID property, AudioChannelLayout layout)
        => SetChannelLayoutProperty(property, &layout);

    internal void RegisterPropertyListener(
        AudioQueuePropertyID property,
        AudioQueuePropertyListenerProc listener,
        nint userData = 0
    ) => AudioQueueException.ThrowIfError(
        NativeMethods.AudioQueueAddPropertyListener(
            queueHandle,
            property,
            listener,
            userData
        )
    );

    internal void RemovePropertyListener(
        AudioQueuePropertyID property,
        AudioQueuePropertyListenerProc listener,
        nint userData = 0
    ) => AudioQueueException.ThrowIfError(
        NativeMethods.AudioQueueRemovePropertyListener(
            queueHandle,
            property,
            listener,
            userData
        )
    );

    #endregion

    #region Playback/recording control

    /// <summary>
    /// Starts playback/recording on the audio queue
    /// </summary>
    protected void Start() => AudioQueueException.ThrowIfError(
        NativeMethods.AudioQueueStart(queueHandle, null)
    );

    /// <summary>
    /// Stops playback/recording on the audio queue
    /// </summary>
    /// <param name="stopImmediately"><see langword="true"/> whether to stop immediately; <see langword="false"/> to stop asynchronously.</param>
    protected void Stop(bool stopImmediately) => AudioQueueException.ThrowIfError(
        NativeMethods.AudioQueueStop(queueHandle, stopImmediately ? MacBoolean.True : MacBoolean.False)
    );

    /// <summary>
    /// Pauses playback/recording on the audio queue
    /// </summary>
    protected void Pause() => AudioQueueException.ThrowIfError(
        NativeMethods.AudioQueuePause(queueHandle)
    );

    #endregion

    #region Buffer management

    internal AudioQueueBuffer* AllocateBuffer(int size)
    {
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueAllocateBuffer(queueHandle, (uint)size, out var buffer)
        );
        return buffer;
    }

    internal void FreeBuffer(AudioQueueBuffer* buffer)
    {
        if (buffer is null) { return; }
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueFreeBuffer(queueHandle, buffer)
        );
    }

    internal void EnqueueBuffer(AudioQueueBuffer* buffer)
    {
        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueEnqueueBuffer(
                queueHandle,
                buffer,
                0U,
                null
            )
        );
    }

    #endregion

    internal bool ObtainAudioTimeStamp(out AudioTimeStamp stamp)
    {
        AudioTimeStamp temp = default;
        bool v = queueHandle != IntPtr.Zero &&
            NativeMethods.AudioQueueGetCurrentTime(queueHandle, IntPtr.Zero, &temp, null) == 0 &&
            temp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid);
        stamp = temp;
        return v;
    }

    internal static double StampToSeconds(in AudioTimeStamp stamp, double sampleRate)
    {
        if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid))
        {
            return stamp.mSampleTime / sampleRate;
        }
        else if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid))
        {
            return CoreAudioFunctions.ConvertHostTimeToSeconds(stamp.mHostTime);
        }
        else
        {
            return 0d;
        }
    }

    /// <summary>
    /// Disposes of all the native resources used by the <see cref="AudioQueueBase"/> class.
    /// </summary>
    /// <param name="disposing">A value whether disposal is done from <see cref="Dispose()"/> or not.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && queueHandle != IntPtr.Zero)
        {
            var osStatus = NativeMethods.AudioQueueDispose(queueHandle, MacBoolean.True);
            queueHandle = IntPtr.Zero;
            AudioQueueException.ThrowIfError(osStatus);
        }
    }

    /// <summary>
    /// Disposes this <see cref="AudioQueueBase"/> object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes this <see cref="AudioQueueBase"/> object, asynchronously.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA1816:Dispose methods should call SuppressFinalize",
        Justification = "GC.SuppressFinalize is implicitly called via the Dispose method"
    )]
    public ValueTask DisposeAsync() => new(Task.Run(new Action(Dispose)));

    /// <summary>
    /// Finalizer for the <see cref="AudioQueueBase"/> class.
    /// </summary>
    ~AudioQueueBase() => Dispose(false);
}