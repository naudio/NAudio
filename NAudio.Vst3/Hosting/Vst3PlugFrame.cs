using System;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side <c>IPlugFrame</c> implementation handed to a plug-in's <c>IPlugView</c> via
/// <c>setFrame</c>. The plug-in calls <see cref="ResizeView"/> when its editor wants to change
/// size (for example the user expands an advanced-settings panel); the host must resize the
/// parent window to match and then call <c>IPlugView::onSize</c> to confirm.
/// </summary>
/// <remarks>
/// Without an <c>IPlugFrame</c> a plug-in cannot resize its own window — many editors call
/// <c>resizeView</c> once during attach to request their natural size, so this is non-optional
/// even for fixed-size editors. The resize callback is marshalled straight back to the owning
/// <see cref="Vst3PluginView"/> via the supplied delegate.
/// </remarks>
[GeneratedComClass]
internal sealed partial class Vst3PlugFrame : IPlugFrame
{
    private readonly Action<ViewRect> _onResize;

    public Vst3PlugFrame(Action<ViewRect> onResize) => _onResize = onResize;

    public int ResizeView(IntPtr view, ref ViewRect newSize)
    {
        _onResize(newSize);
        return TResultCodes.Ok;
    }
}
