using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression;

/// <summary>
/// Interop definitions for the Windows ACM (Audio Compression Manager) API.
/// </summary>
/// <remarks>
/// We have observed access violations inside <c>msacm32.dll</c> when ACM
/// calls are issued concurrently from multiple threads — see GitHub issues
/// <see href="https://github.com/naudio/NAudio/issues/355"/> and
/// <see href="https://github.com/naudio/NAudio/issues/629"/>. It isn't
/// clear whether every ACM codec is affected or only some, but as a
/// defensive measure every msacm32 entry point exposed by this class is
/// serialised behind <see cref="AcmLock"/>.
/// <para>
/// Convention for adding a new P/Invoke: declare the native call as
/// <c>private static extern MmResult Native_acmFoo(...)</c> with an
/// explicit <c>EntryPoint</c>, then add a matching
/// <c>public static MmResult acmFoo(...)</c> wrapper that takes
/// <see cref="AcmLock"/> and forwards to it.
/// </para>
/// <para>
/// The lock is reentrant (<see cref="System.Threading.Monitor"/>), so
/// msacm32 callbacks such as <see cref="AcmDriverEnumCallback"/>,
/// <see cref="AcmFormatEnumCallback"/> and
/// <see cref="AcmFormatTagEnumCallback"/> are free to call back into this
/// class from inside the user-supplied callback without deadlocking.
/// </para>
/// </remarks>
class AcmInterop
{
    /// <summary>
    /// Process-wide lock guarding every msacm32 P/Invoke. See the class
    /// remarks on <see cref="AcmInterop"/> for why this exists.
    /// </summary>
    internal static readonly object AcmLock = new object();

    // http://msdn.microsoft.com/en-us/library/dd742891%28VS.85%29.aspx
    public delegate bool AcmDriverEnumCallback(IntPtr hAcmDriverId, IntPtr instance, AcmDriverDetailsSupportFlags flags);

    public delegate bool AcmFormatEnumCallback(IntPtr hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

    public delegate bool AcmFormatTagEnumCallback(IntPtr hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

    /// <summary>
    /// http://msdn.microsoft.com/en-us/library/dd742910%28VS.85%29.aspx
    /// UINT ACMFORMATCHOOSEHOOKPROC acmFormatChooseHookProc(
    ///   HWND hwnd,
    ///   UINT uMsg,
    ///   WPARAM wParam,
    ///   LPARAM lParam
    /// </summary>
    public delegate bool AcmFormatChooseHookProc(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

    // not done:
    // acmDriverID
    // acmDriverMessage
    // acmFilterChoose
    // acmFilterChooseHookProc
    // acmFilterDetails
    // acmFilterEnum -acmFilterEnumCallback
    // acmFilterTagDetails
    // acmFilterTagEnum
    // acmFormatDetails
    // acmFormatTagDetails
    // acmGetVersion
    // acmStreamMessage

    // http://msdn.microsoft.com/en-us/library/windows/desktop/dd742885%28v=vs.85%29.aspx
    // MMRESULT acmDriverAdd(
    //        LPHACMDRIVERID phadid,
    //        HINSTANCE hinstModule,
    //        LPARAM lParam,
    //        DWORD dwPriority,
    //        DWORD fdwAdd)
    [DllImport("msacm32.dll", EntryPoint = "acmDriverAdd")]
    private static extern MmResult Native_acmDriverAdd(out IntPtr driverHandle,
        IntPtr driverModule,
        IntPtr driverFunctionAddress,
        int priority,
        AcmDriverAddFlags flags);

    public static MmResult acmDriverAdd(out IntPtr driverHandle,
        IntPtr driverModule,
        IntPtr driverFunctionAddress,
        int priority,
        AcmDriverAddFlags flags)
    {
        lock (AcmLock) return Native_acmDriverAdd(out driverHandle, driverModule, driverFunctionAddress, priority, flags);
    }

    // http://msdn.microsoft.com/en-us/library/windows/desktop/dd742897%28v=vs.85%29.aspx
    [DllImport("msacm32.dll", EntryPoint = "acmDriverRemove")]
    private static extern MmResult Native_acmDriverRemove(IntPtr driverHandle, int removeFlags);

    public static MmResult acmDriverRemove(IntPtr driverHandle, int removeFlags)
    {
        lock (AcmLock) return Native_acmDriverRemove(driverHandle, removeFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742886%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmDriverClose")]
    private static extern MmResult Native_acmDriverClose(IntPtr hAcmDriver, int closeFlags);

    public static MmResult acmDriverClose(IntPtr hAcmDriver, int closeFlags)
    {
        lock (AcmLock) return Native_acmDriverClose(hAcmDriver, closeFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742890%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmDriverEnum")]
    private static extern MmResult Native_acmDriverEnum(AcmDriverEnumCallback fnCallback, IntPtr dwInstance, AcmDriverEnumFlags flags);

    public static MmResult acmDriverEnum(AcmDriverEnumCallback fnCallback, IntPtr dwInstance, AcmDriverEnumFlags flags)
    {
        lock (AcmLock) return Native_acmDriverEnum(fnCallback, dwInstance, flags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742887%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmDriverDetails")]
    private static extern MmResult Native_acmDriverDetails(IntPtr hAcmDriver, ref AcmDriverDetails driverDetails, int reserved);

    public static MmResult acmDriverDetails(IntPtr hAcmDriver, ref AcmDriverDetails driverDetails, int reserved)
    {
        lock (AcmLock) return Native_acmDriverDetails(hAcmDriver, ref driverDetails, reserved);
    }

    // http://msdn.microsoft.com/en-us/library/dd742894%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmDriverOpen")]
    private static extern MmResult Native_acmDriverOpen(out IntPtr pAcmDriver, IntPtr hAcmDriverId, int openFlags);

    public static MmResult acmDriverOpen(out IntPtr pAcmDriver, IntPtr hAcmDriverId, int openFlags)
    {
        lock (AcmLock) return Native_acmDriverOpen(out pAcmDriver, hAcmDriverId, openFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742909%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmFormatChooseW")]
    private static extern MmResult Native_acmFormatChoose(ref AcmFormatChoose formatChoose);

    public static MmResult acmFormatChoose(ref AcmFormatChoose formatChoose)
    {
        // Note: this is a modal dialog; the lock is held for as long as the
        // user has the dialog open. Concurrent ACM operations on other
        // threads will block until the dialog is dismissed. That is the
        // safe default given msacm32's thread-unsafety.
        lock (AcmLock) return Native_acmFormatChoose(ref formatChoose);
    }

    // http://msdn.microsoft.com/en-us/library/dd742914%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmFormatEnum")]
    private static extern MmResult Native_acmFormatEnum(IntPtr hAcmDriver, ref AcmFormatDetails formatDetails, AcmFormatEnumCallback callback, IntPtr instance, AcmFormatEnumFlags flags);

    public static MmResult acmFormatEnum(IntPtr hAcmDriver, ref AcmFormatDetails formatDetails, AcmFormatEnumCallback callback, IntPtr instance, AcmFormatEnumFlags flags)
    {
        lock (AcmLock) return Native_acmFormatEnum(hAcmDriver, ref formatDetails, callback, instance, flags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742916%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmFormatSuggest")]
    private static extern MmResult Native_acmFormatSuggest2(
        IntPtr hAcmDriver,
        IntPtr sourceFormatPointer,
        IntPtr destFormatPointer,
        int sizeDestFormat,
        AcmFormatSuggestFlags suggestFlags);

    public static MmResult acmFormatSuggest2(
        IntPtr hAcmDriver,
        IntPtr sourceFormatPointer,
        IntPtr destFormatPointer,
        int sizeDestFormat,
        AcmFormatSuggestFlags suggestFlags)
    {
        lock (AcmLock) return Native_acmFormatSuggest2(hAcmDriver, sourceFormatPointer, destFormatPointer, sizeDestFormat, suggestFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742919%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmFormatTagEnum")]
    private static extern MmResult Native_acmFormatTagEnum(IntPtr hAcmDriver, ref AcmFormatTagDetails formatTagDetails, AcmFormatTagEnumCallback callback, IntPtr instance, int reserved);

    public static MmResult acmFormatTagEnum(IntPtr hAcmDriver, ref AcmFormatTagDetails formatTagDetails, AcmFormatTagEnumCallback callback, IntPtr instance, int reserved)
    {
        lock (AcmLock) return Native_acmFormatTagEnum(hAcmDriver, ref formatTagDetails, callback, instance, reserved);
    }

    // http://msdn.microsoft.com/en-us/library/dd742922%28VS.85%29.aspx
    // this version of the prototype is for metrics that output a single integer
    [DllImport("Msacm32.dll", EntryPoint = "acmMetrics")]
    private static extern MmResult Native_acmMetrics(IntPtr hAcmObject, AcmMetrics metric, out int output);

    public static MmResult acmMetrics(IntPtr hAcmObject, AcmMetrics metric, out int output)
    {
        lock (AcmLock) return Native_acmMetrics(hAcmObject, metric, out output);
    }

    // http://msdn.microsoft.com/en-us/library/dd742928%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamOpen")]
    private static extern MmResult Native_acmStreamOpen2(
        out IntPtr hAcmStream,
        IntPtr hAcmDriver,
        IntPtr sourceFormatPointer,
        IntPtr destFormatPointer,
        [In] WaveFilter waveFilter,
        IntPtr callback,
        IntPtr instance,
        AcmStreamOpenFlags openFlags);

    public static MmResult acmStreamOpen2(
        out IntPtr hAcmStream,
        IntPtr hAcmDriver,
        IntPtr sourceFormatPointer,
        IntPtr destFormatPointer,
        WaveFilter waveFilter,
        IntPtr callback,
        IntPtr instance,
        AcmStreamOpenFlags openFlags)
    {
        lock (AcmLock) return Native_acmStreamOpen2(out hAcmStream, hAcmDriver, sourceFormatPointer, destFormatPointer, waveFilter, callback, instance, openFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742923%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamClose")]
    private static extern MmResult Native_acmStreamClose(IntPtr hAcmStream, int closeFlags);

    public static MmResult acmStreamClose(IntPtr hAcmStream, int closeFlags)
    {
        lock (AcmLock) return Native_acmStreamClose(hAcmStream, closeFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742924%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamConvert")]
    private static extern MmResult Native_acmStreamConvert(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, AcmStreamConvertFlags streamConvertFlags);

    public static MmResult acmStreamConvert(IntPtr hAcmStream, AcmStreamHeaderStruct streamHeader, AcmStreamConvertFlags streamConvertFlags)
    {
        lock (AcmLock) return Native_acmStreamConvert(hAcmStream, streamHeader, streamConvertFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742929%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamPrepareHeader")]
    private static extern MmResult Native_acmStreamPrepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int prepareFlags);

    public static MmResult acmStreamPrepareHeader(IntPtr hAcmStream, AcmStreamHeaderStruct streamHeader, int prepareFlags)
    {
        lock (AcmLock) return Native_acmStreamPrepareHeader(hAcmStream, streamHeader, prepareFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742929%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamReset")]
    private static extern MmResult Native_acmStreamReset(IntPtr hAcmStream, int resetFlags);

    public static MmResult acmStreamReset(IntPtr hAcmStream, int resetFlags)
    {
        lock (AcmLock) return Native_acmStreamReset(hAcmStream, resetFlags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742931%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamSize")]
    private static extern MmResult Native_acmStreamSize(IntPtr hAcmStream, int inputBufferSize, out int outputBufferSize, AcmStreamSizeFlags flags);

    public static MmResult acmStreamSize(IntPtr hAcmStream, int inputBufferSize, out int outputBufferSize, AcmStreamSizeFlags flags)
    {
        lock (AcmLock) return Native_acmStreamSize(hAcmStream, inputBufferSize, out outputBufferSize, flags);
    }

    // http://msdn.microsoft.com/en-us/library/dd742932%28VS.85%29.aspx
    [DllImport("Msacm32.dll", EntryPoint = "acmStreamUnprepareHeader")]
    private static extern MmResult Native_acmStreamUnprepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int flags);

    public static MmResult acmStreamUnprepareHeader(IntPtr hAcmStream, AcmStreamHeaderStruct streamHeader, int flags)
    {
        lock (AcmLock) return Native_acmStreamUnprepareHeader(hAcmStream, streamHeader, flags);
    }
}
