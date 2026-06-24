using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using NAudio.Vst3.Hosting;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3;

/// <summary>
/// The pixel size of a plug-in editor view.
/// </summary>
public readonly record struct Vst3ViewSize(int Width, int Height);

/// <summary>
/// A VST 3® plug-in's embedded editor window (its GUI). Wraps the native <c>IPlugView</c> and
/// the host-side <c>IPlugFrame</c> callback.
/// </summary>
/// <remarks>
/// <para>
/// Windows-only in this release: the view is parented to an <c>HWND</c> supplied by the host UI
/// framework — a WPF <c>HwndHost</c>, a WinForms <c>Control.Handle</c>, or a bare Win32 window.
/// Obtain an instance from <see cref="Vst3Plugin.CreateView"/>.
/// </para>
/// <para>
/// <b>Threading.</b> Every member must be called on the host's UI (STA) thread — the same thread
/// that created the view. The VST 3 threading contract forbids touching the editor from the audio
/// thread; violations crash inside plug-in code in ways that masquerade as plug-in bugs but are
/// host bugs. The creating thread is captured at construction and asserted on each entry point.
/// </para>
/// <para>
/// <b>Lifetime.</b> Dispose the view <b>before</b> the owning <see cref="Vst3Plugin"/>. The native
/// view is owned by the controller; releasing it after the controller has terminated is undefined.
/// </para>
/// </remarks>
public sealed unsafe class Vst3PluginView : IDisposable
{
    private readonly Thread _uiThread;
    private IPlugView? _view;
    private IntPtr _viewPtr;
    private Vst3PlugFrame? _plugFrame;
    private IntPtr _plugFrameUnknown;
    private bool _attached;

    /// <summary>
    /// Raised when the plug-in asks the host to resize its window (<c>IPlugFrame::resizeView</c>).
    /// The host should resize the parent window to the supplied size; the view is told the new
    /// size automatically after the event returns. Raised on the UI thread.
    /// </summary>
    public event EventHandler<Vst3ViewSize>? Resized;

    internal Vst3PluginView(IntPtr viewPtr)
    {
        _uiThread = Thread.CurrentThread;
        _viewPtr = viewPtr;
        _view = (IPlugView)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
            viewPtr, CreateObjectFlags.UniqueInstance);

        // Hand the view a host-side IPlugFrame so it can request resizes. Even fixed-size editors
        // typically call resizeView once during attach to declare their natural size.
        //
        // As with IEditController::setComponentHandler (see Vst3CcwInteropCrash.md),
        // IPlugView::setFrame stores the pointer verbatim — the plug-in calls resizeView on it
        // without QI-ing it back. So we must hand over the IPlugFrame interface dispatch, NOT the
        // CCW's bare IUnknown identity that GetOrCreateComInterfaceForObject returns (calling
        // resizeView on the 3-slot IUnknown vtable would over-read into adjacent ComWrappers memory).
        _plugFrame = new Vst3PlugFrame(OnPlugRequestedResize);
        var frameIdentity = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            _plugFrame, CreateComInterfaceFlags.None);
        try
        {
            var iid = Vst3StandardInterfaceIds.IPlugFrame;
            var hr = Marshal.QueryInterface(frameIdentity, in iid, out _plugFrameUnknown);
            if (hr != 0 || _plugFrameUnknown == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to QI host-side IPlugFrame (HRESULT 0x{hr:X8}).");
            }
        }
        finally
        {
            Marshal.Release(frameIdentity);
        }
        _view.SetFrame(_plugFrameUnknown);
    }

    /// <summary>
    /// Embeds the editor in the supplied parent window. Pushes the host's DPI scale to the view
    /// (if the view supports <c>IPlugViewContentScaleSupport</c>) before attaching.
    /// </summary>
    /// <param name="parentHwnd">The parent window handle to host the editor under.</param>
    /// <param name="contentScaleFactor">
    /// The host's DPI scale (1.0 at 96 DPI, 1.25 at 120 DPI, 2.0 at 192 DPI). Critical on
    /// high-DPI displays — without it many editors render at the wrong size.
    /// </param>
    /// <exception cref="NotSupportedException">The plug-in view does not support HWND embedding.</exception>
    public void AttachTo(IntPtr parentHwnd, float contentScaleFactor = 1.0f)
    {
        EnsureUiThread();
        var view = _view ?? throw new ObjectDisposedException(nameof(Vst3PluginView));
        if (parentHwnd == IntPtr.Zero)
        {
            throw new ArgumentException("Parent window handle must be non-zero.", nameof(parentHwnd));
        }
        if (_attached)
        {
            throw new InvalidOperationException("View is already attached to a parent window.");
        }

        ReadOnlySpan<byte> hwndType = "HWND\0"u8;
        fixed (byte* typePtr = hwndType)
        {
            if (view.IsPlatformTypeSupported((IntPtr)typePtr) != TResultCodes.Ok)
            {
                throw new NotSupportedException(
                    "Plug-in view does not support HWND embedding (this release hosts editors on Windows only).");
            }

            // DPI scale must be pushed before attached() so the plug-in lays out at the right size.
            TrySetContentScale(contentScaleFactor);

            var hr = view.Attached(parentHwnd, (IntPtr)typePtr);
            if (hr != TResultCodes.Ok)
            {
                throw new InvalidOperationException($"IPlugView::attached failed (HRESULT 0x{hr:X8}).");
            }
        }
        _attached = true;
    }

    /// <summary>Current size the plug-in reports for its editor (<c>IPlugView::getSize</c>).</summary>
    public Vst3ViewSize GetSize()
    {
        EnsureUiThread();
        var view = _view ?? throw new ObjectDisposedException(nameof(Vst3PluginView));
        return view.GetSize(out var rect) == TResultCodes.Ok
            ? new Vst3ViewSize(rect.Width, rect.Height)
            : default;
    }

    /// <summary>
    /// <c>true</c> when the plug-in advertises a resizable editor (<c>IPlugView::canResize</c>).
    /// Fixed-size editors return <c>false</c>; the host should not let the user resize the window.
    /// </summary>
    public bool CanResize()
    {
        EnsureUiThread();
        var view = _view ?? throw new ObjectDisposedException(nameof(Vst3PluginView));
        return view.CanResize() == TResultCodes.Ok;
    }

    /// <summary>
    /// Tells the plug-in the editor was resized by the host (<c>IPlugView::onSize</c>). Call this
    /// when the user resizes the host window, after honouring <see cref="CheckSizeConstraint"/>.
    /// </summary>
    public void SetSize(int width, int height)
    {
        EnsureUiThread();
        var view = _view ?? throw new ObjectDisposedException(nameof(Vst3PluginView));
        var rect = new ViewRect { Left = 0, Top = 0, Right = width, Bottom = height };
        view.OnSize(ref rect);
    }

    /// <summary>
    /// Asks the plug-in to clamp a proposed size to one it can actually display
    /// (<c>IPlugView::checkSizeConstraint</c>). Returns the (possibly adjusted) size.
    /// </summary>
    public Vst3ViewSize CheckSizeConstraint(int width, int height)
    {
        EnsureUiThread();
        var view = _view ?? throw new ObjectDisposedException(nameof(Vst3PluginView));
        var rect = new ViewRect { Left = 0, Top = 0, Right = width, Bottom = height };
        view.CheckSizeConstraint(ref rect);
        return new Vst3ViewSize(rect.Width, rect.Height);
    }

    /// <summary>Pushes an updated DPI scale to the view at runtime (e.g. the window moved monitors).</summary>
    public void SetContentScaleFactor(float factor)
    {
        EnsureUiThread();
        TrySetContentScale(factor);
    }

    /// <summary>
    /// Detaches the editor from its parent window (<c>IPlugView::removed</c>). Call this <b>before</b>
    /// destroying the parent <c>HWND</c> — the SDK requires the view be removed while its parent is
    /// still valid. Idempotent and also invoked by <see cref="Dispose"/>; safe to call if never
    /// attached.
    /// </summary>
    public void Detach()
    {
        EnsureUiThread();
        if (_view is null || !_attached)
        {
            return;
        }
        try { _view.Removed(); } catch { /* best effort — parent may already be gone */ }
        _attached = false;
    }

    private void OnPlugRequestedResize(ViewRect rect)
    {
        // Per the SDK resizeView contract: the host resizes the parent window to the requested
        // size (our subscribers do that synchronously), then we confirm the new size to the view.
        Resized?.Invoke(this, new Vst3ViewSize(rect.Width, rect.Height));
        var confirmed = rect;
        _view?.OnSize(ref confirmed);
    }

    private void TrySetContentScale(float factor)
    {
        if (_viewPtr == IntPtr.Zero)
        {
            return;
        }
        var iid = Vst3StandardInterfaceIds.IPlugViewContentScaleSupport;
        if (Marshal.QueryInterface(_viewPtr, in iid, out var scalePtr) != 0 || scalePtr == IntPtr.Zero)
        {
            // View doesn't implement content-scale support — fine, it'll use the OS default.
            return;
        }
        try
        {
            var scale = (IPlugViewContentScaleSupport)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
                scalePtr, CreateObjectFlags.UniqueInstance);
            scale.SetContentScaleFactor(factor);
            ((ComObject)(object)scale).FinalRelease();
        }
        finally
        {
            Marshal.Release(scalePtr);
        }
    }

    private void EnsureUiThread()
    {
        Debug.Assert(
            Thread.CurrentThread == _uiThread,
            "Vst3PluginView must be used on the UI (STA) thread it was created on.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_view is not null)
        {
            Detach();
            try { _view.SetFrame(IntPtr.Zero); } catch { /* best effort */ }
            ((ComObject)(object)_view).FinalRelease();
            _view = null;
        }
        if (_viewPtr != IntPtr.Zero)
        {
            Marshal.Release(_viewPtr);
            _viewPtr = IntPtr.Zero;
        }
        if (_plugFrameUnknown != IntPtr.Zero)
        {
            Marshal.Release(_plugFrameUnknown);
            _plugFrameUnknown = IntPtr.Zero;
        }
        _plugFrame = null;
    }
}
