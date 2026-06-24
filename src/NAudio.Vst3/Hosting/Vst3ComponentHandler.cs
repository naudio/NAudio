using System;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3.Hosting;

/// <summary>
/// Host-side <c>IComponentHandler</c> (+ <c>IComponentHandler2</c>) implementation passed to a
/// plug-in's <c>IEditController</c> via <c>setComponentHandler</c>. <c>PerformEdit</c> routes
/// editor-driven parameter changes back into the owning <see cref="Vst3Plugin"/> so the audio
/// thread sees them on the next <c>process()</c> call via <c>IParameterChanges</c>.
/// </summary>
/// <remarks>
/// JUCE-wrapped plug-ins (e.g. Tonex, ValhallaVintageVerb) pump their own UI→DSP edits through an
/// internal value tree, so they appear to work even when the host drops <c>PerformEdit</c>;
/// plug-ins built on the Steinberg SDK helpers (e.g. Arturia) genuinely depend on the host
/// forwarding the edit through <c>inputParameterChanges</c> for the DSP to receive it.
/// </remarks>
[GeneratedComClass]
internal sealed partial class Vst3ComponentHandler : IComponentHandler, IComponentHandler2
{
    private readonly Action<uint, double>? _onPerformEdit;
    private readonly Action<int>? _onRestart;

    public Vst3ComponentHandler(Action<uint, double>? onPerformEdit = null, Action<int>? onRestart = null)
    {
        _onPerformEdit = onPerformEdit;
        _onRestart = onRestart;
    }

    public int BeginEdit(uint id) => TResultCodes.Ok;

    public int PerformEdit(uint id, double valueNormalized)
    {
        _onPerformEdit?.Invoke(id, valueNormalized);
        return TResultCodes.Ok;
    }

    public int EndEdit(uint id) => TResultCodes.Ok;

    // Plug-ins call this after setState (and other restart-worthy events) to ask the host to
    // re-query parameter values, latency, bus configuration, etc. kNotImplemented is a
    // documented-valid response but some plug-ins interpret it as "host can't accept the restart"
    // and roll the change back, so we always return Ok. The owning Vst3Plugin gets the flags via
    // _onRestart and acts on the ones it handles (e.g. kLatencyChanged → re-query latency).
    public int RestartComponent(int flags)
    {
        _onRestart?.Invoke(flags);
        return TResultCodes.Ok;
    }

    public int SetDirty(byte state) => TResultCodes.Ok;
    public int RequestOpenEditor(System.IntPtr name) => TResultCodes.NotImplemented;
    public int StartGroupEdit() => TResultCodes.Ok;
    public int FinishGroupEdit() => TResultCodes.Ok;
}
