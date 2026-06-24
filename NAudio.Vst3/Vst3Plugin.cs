using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using NAudio.Vst3.Hosting;
using NAudio.Vst3.Interop;

namespace NAudio.Vst3;

/// <summary>
/// An instantiated, activated, processing-ready VST 3® audio-effect plug-in.
/// </summary>
/// <remarks>
/// <para>
/// Phase 2 surface — stereo-in / stereo-out audio effects only. Plug-ins that cannot accept a
/// stereo bus arrangement, or that do not have at least one audio input and one audio output
/// bus, are rejected at construction with an <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// Lifecycle: instantiation runs the full SDK start-up dance — <c>createInstance</c> →
/// <c>queryInterface(IAudioProcessor)</c> → <c>initialize</c> → <c>setBusArrangements(stereo)</c>
/// → <c>setupProcessing</c> → <c>activateBus(in)</c> → <c>activateBus(out)</c> →
/// <c>setActive(true)</c> → <c>setProcessing(true)</c>. <see cref="Dispose"/> tears down in
/// reverse. <see cref="Process"/> may be called repeatedly between those two.
/// </para>
/// <para>
/// The owning <see cref="Vst3Module"/> must outlive every <see cref="Vst3Plugin"/> it creates.
/// </para>
/// <para>
/// <b>Threading and disposal.</b> <see cref="Process"/> runs on the audio thread, while the
/// lifecycle, parameter and state calls run on the host's control thread. <see cref="Dispose"/>
/// is <b>not</b> synchronised against an in-flight <see cref="Process"/> — disposing while the
/// audio thread is inside <see cref="Process"/> releases the native objects underneath it. Stop the
/// plug-in and remove it from the audio graph (so no further <see cref="Process"/> call can run)
/// before disposing it.
/// </para>
/// </remarks>
public sealed unsafe class Vst3Plugin : IDisposable
{
    private IComponent? _component;
    private IAudioProcessor? _processor;
    private IEditController? _controller;
    private Vst3HostApplication? _hostApp;
    private IntPtr _hostUnknown;
    private IntPtr _componentHandlerUnknown;

    // Raw IConnectionPoint pointers cached up-front when we discover them — needed because
    // IConnectionPoint::Connect takes a native IConnectionPoint*, not an IUnknown, and lifting
    // it out of a managed RCW is awkward. We keep these AddRef'd until Dispose.
    private IntPtr _componentCpPtr;
    private IntPtr _controllerCpPtr;

    // Negotiated channel counts and speaker masks from setBusArrangements. Filled by
    // NegotiateBusArrangement during init. Channel counts are 1 or 2 only — wider arrangements
    // are rejected at init time. _inputBusCount / _outputBusCount are the plug-in's total bus
    // count (which we always pass to ProcessData via NumInputs / NumOutputs — many plug-ins
    // assert/misbehave when these don't match getBusCount), even though we only feed audio
    // through bus 0 and explicitly deactivate the rest.
    private int _inputChannels;
    private int _outputChannels;
    private ulong _inputArrangement;
    private ulong _outputArrangement;
    private int _inputBusCount;
    private int _outputBusCount;

    // Per-channel float* buffers, allocated based on negotiated channel counts.
    private float** _inputBuffers;
    private float** _outputBuffers;
    private IntPtr* _inputChannelPtrs;
    private IntPtr* _outputChannelPtrs;
    private AudioBusBuffers* _inputBus;
    private AudioBusBuffers* _outputBus;

    private Vst3HostParameterChanges? _inputChanges;
    private IntPtr _inputChangesPtr;
    private ulong _lastOutputSilenceFlags;

    // Instrument (VSTi) event input — built only when the plug-in is an instrument. Notes are
    // scheduled against an absolute sample timeline (_samplePosition advances each Process block);
    // Process drains the events falling within the current block into _inputEvents and feeds them
    // via ProcessData.InputEvents. Effects leave all of this unused (InputEvents/ProcessContext
    // stay null, preserving the exact effect behaviour validated in earlier phases).
    private readonly bool _isInstrument;
    private Vst3HostEventList? _inputEvents;
    private IntPtr _inputEventsPtr;
    private bool _eventInputActive;
    private readonly List<ScheduledEvent> _scheduledEvents = new();
    private int _scheduleCursor;
    private bool _scheduleSorted = true;
    private long _samplePosition;
    // Live (realtime) note input. Enqueued from any thread (e.g. a MIDI callback) and drained by
    // Process on the audio thread, applied at the start of the next block. Separate from the
    // (offline, sorted, single-threaded) _scheduledEvents path above.
    //
    // Each entry carries the absolute target sample at which the event should fire — computed at
    // enqueue time on the producer thread from the captured block-start clock snapshot below. The
    // audio thread drains into _pendingLiveEvents at the top of each Process call and dispatches
    // only those targets that fall within the current block; entries past the block end stay for
    // the next round. Without that target-sample carry, every event would fire at sample 0 of the
    // next block — i.e. one block's worth of timing jitter (±5–10 ms at typical buffer sizes).
    private readonly ConcurrentQueue<(long TargetSample, Event Ev)> _liveEvents = new();
    private readonly List<(long TargetSample, Event Ev)> _pendingLiveEvents = new();
    // Currently-sounding notes (key = channel<<8 | pitch), so AllNotesOff can release them.
    private readonly ConcurrentDictionary<int, byte> _activeNotes = new();

    // Segment-driven event input (offline render / SequencedMidiPlayer via Vst3MidiInstrument):
    // notes/controllers that must fire at sample offset 0 of the next Process call, in arrival order.
    // Unlike _liveEvents these carry no wall-clock target — the host has already split the audio block
    // at each event's frame, so every queued event belongs at the start of the next (sub-)block. That
    // makes them correct under faster-than-real-time rendering, where the Stopwatch clock the Send*
    // path relies on has no meaning. Drained FIFO each block; controllers route to parameter changes
    // exactly as the live path does. See EnqueueNoteOn / Vst3MidiInstrument.
    private readonly ConcurrentQueue<Event> _immediateEvents = new();
    private readonly ConcurrentQueue<(uint Id, double Value)> _immediateParamChanges = new();

    // SysEx (Data) events carry a pointer to an unmanaged byte buffer that must stay alive until the
    // process() call that consumes them returns. Buffers are allocated when a SysEx event is built and
    // recorded here as each Data event is handed to the plug-in this block, then freed once process()
    // returns. Any still sitting un-dispatched in the event queues are freed on dispose.
    private readonly List<IntPtr> _sysexBuffersThisBlock = new();

    // Musical/transport context for the ProcessContext fed to instruments each block. Null until a
    // host (e.g. Vst3MidiInstrument driven by a sequencer Transport) pushes one via SetMusicalContext,
    // in which case BuildProcessContext emits the pushed tempo / time-signature / position / playing
    // state instead of the free-running 120-BPM stopped default. Set and read on the audio thread.
    private Vst3MusicalContext? _musicalContext;

    // Clock-snapshot for translating producer-side arrival timestamps into target sample positions.
    // Updated at the top of every Process call (audio thread). Read under the lock by producers
    // computing target samples in Send* methods. Lock contention is microsecond-scale (two stores
    // on each side) and only happens when a MIDI event arrives — fine for the audio thread.
    private readonly object _clockLock = new();
    private long _blockStartSample;
    private long _blockStartTimestampTicks;

    // MIDI controller → parameter routing. VST 3 delivers CC / pitch-bend / mod-wheel / aftertouch
    // as *parameter changes*, not events: the controller's IMidiMapping resolves each controller to
    // a parameter id (resolved once into _midiCcToParam at init), and live changes are queued from
    // any thread into _liveParamChanges and drained into the IParameterChanges each block. Same
    // target-sample carry as _liveEvents so editor-thread or MIDI-thread CC tweaks land at the
    // right sample offset within the block.
    private IntPtr _midiMappingPtr;
    private readonly Dictionary<short, uint> _midiCcToParam = new();

    // Program change. VST 3 has no program-change event: the host drives the parameter flagged
    // IsProgramChange (the unit's program-list selector) to a normalised value. Resolved once from the
    // parameter list at init; a MIDI program N maps to normalised N / StepCount (the program count - 1).
    private bool _hasProgramChange;
    private uint _programChangeParamId;
    private int _programChangeStepCount;
    private int _programChangeUnitId;
    private readonly ConcurrentQueue<(long TargetSample, uint Id, double Value)> _liveParamChanges = new();
    private readonly List<(long TargetSample, uint Id, double Value)> _pendingLiveParamChanges = new();

    // Unit / program-list enumeration (Vst::IUnitInfo, optional). QI'd alongside the controller, then
    // consumed by BuildUnitModel into the public Units / ProgramLists collections.
    private IntPtr _unitInfoPtr;

    private bool _initialized;
    private bool _controllerInitialized;
    private bool _hasSeparateController;
    private bool _connected;
    private bool _active;
    private bool _processing;

    /// <summary>The class descriptor this plug-in was instantiated from.</summary>
    public Vst3ClassInfo ClassInfo { get; }

    /// <summary>Sample rate configured at <c>setupProcessing</c> time.</summary>
    public int SampleRate { get; }

    /// <summary>Maximum samples per <see cref="Process"/> block.</summary>
    public int MaxBlockSize { get; }

    /// <summary>
    /// Number of input channels negotiated with the plug-in via <c>setBusArrangements</c>. The host
    /// first tries stereo; if the plug-in refuses, we accept its declared default and use that
    /// arrangement instead. Mono (1) and stereo (2) are the only counts currently supported.
    /// </summary>
    public int InputChannelCount => _inputChannels;

    /// <summary>
    /// Number of output channels negotiated with the plug-in via <c>setBusArrangements</c>. See
    /// <see cref="InputChannelCount"/> for the negotiation strategy.
    /// </summary>
    public int OutputChannelCount => _outputChannels;

    /// <summary>
    /// Reported by <c>IAudioProcessor::getLatencySamples</c> after activation. A plug-in may change
    /// its latency at runtime (e.g. switching an EQ to linear phase, or enabling oversampling); when
    /// it does it raises <c>restartComponent(kLatencyChanged)</c>, which re-queries this value and
    /// fires <see cref="LatencyChanged"/>. Reads are atomic (volatile).
    /// </summary>
    public uint LatencySamples => (uint)Volatile.Read(ref _latencySamples);
    private int _latencySamples;

    /// <summary>
    /// Raised when the plug-in reports a latency change at runtime (via
    /// <c>restartComponent(kLatencyChanged)</c>) and <see cref="LatencySamples"/> has been updated.
    /// A host wiring this for live playback should treat it as "rebuild the processing graph": the new
    /// latency only takes audible effect once downstream delay compensation is recomputed. Fired on the
    /// thread the plug-in calls <c>restartComponent</c> from (typically its UI/edit thread), so a
    /// handler that touches the audio graph must marshal as needed.
    /// </summary>
    public event EventHandler? LatencyChanged;

    /// <summary>
    /// Reported by <c>IAudioProcessor::getTailSamples</c> after activation. <c>0</c> = no tail;
    /// <c>uint.MaxValue</c> = infinite (sustained reverb, feedback loops). Useful for offline
    /// rendering to know how much silence to feed after the input ends.
    /// </summary>
    public uint TailSamples { get; private set; }

    /// <summary>
    /// Every parameter the plug-in's edit controller advertises, in declaration order. The
    /// collection itself is built once at construction; individual <see cref="Vst3Parameter"/>
    /// values are live and round-trip to the controller on each property access.
    /// </summary>
    public Vst3ParameterCollection Parameters { get; private set; } = null!;

    /// <summary>
    /// The plug-in's units (the <c>IUnitInfo</c> hierarchy), or an empty list when the plug-in does
    /// not implement <c>IUnitInfo</c>. The root unit (id 0) is present whenever any unit is.
    /// </summary>
    public IReadOnlyList<Vst3Unit> Units { get; private set; } = Array.Empty<Vst3Unit>();

    /// <summary>
    /// The plug-in's program lists (its factory presets, grouped), or an empty list when the plug-in
    /// does not implement <c>IUnitInfo</c>. Select a program with <see cref="SendProgramChange"/> /
    /// <see cref="EnqueueProgramChange"/>; read the current one via <see cref="CurrentProgram"/>.
    /// </summary>
    public IReadOnlyList<Vst3ProgramList> ProgramLists { get; private set; } = Array.Empty<Vst3ProgramList>();

    /// <summary>
    /// The program list the program-change parameter selects from — resolved via that parameter's
    /// owning unit, or the sole list when there is exactly one. <c>null</c> when the plug-in exposes
    /// no program list (or no program-change parameter to tie one to).
    /// </summary>
    public Vst3ProgramList? ActiveProgramList { get; private set; }

    /// <summary>
    /// The currently-selected program index (the inverse of what <see cref="SendProgramChange"/>
    /// writes), or <c>-1</c> when the plug-in has no program-change parameter. Read from the
    /// parameter's current normalised value, so it reflects program changes the plug-in's own UI made.
    /// </summary>
    public int CurrentProgram
    {
        get
        {
            if (!_hasProgramChange) return -1;
            var span = _programChangeStepCount > 0 ? _programChangeStepCount : 127;
            var normalized = GetParameterNormalized(_programChangeParamId);
            return (int)Math.Round(normalized * span);
        }
    }

    /// <summary>
    /// <c>true</c> when the plug-in's <c>IEditController</c> is a separate object from its
    /// <c>IComponent</c> (the two-object model). Informational only — both shapes are handled
    /// transparently by the public API.
    /// </summary>
    public bool HasSeparateController => _hasSeparateController;

    internal Vst3Plugin(IPluginFactory factory, Vst3ClassInfo classInfo, int sampleRate, int maxBlockSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBlockSize);

        ClassInfo = classInfo;
        _isInstrument = classInfo.IsInstrument;
        SampleRate = sampleRate;
        MaxBlockSize = maxBlockSize;

        try
        {
            Initialise(factory);
        }
        catch
        {
            DisposeCore();
            throw;
        }
    }

    private void Initialise(IPluginFactory factory)
    {
        // 1. Resolve CID + IID and create the component instance.
        Span<byte> cidBytes = stackalloc byte[16];
        Vst3Tuid.Parse(ClassInfo.ClassId, cidBytes);
        var componentIidBytes = Vst3StandardInterfaceIds.IComponent.ToByteArray(bigEndian: false);

        IntPtr componentPtr;
        fixed (byte* cidPtr = cidBytes)
        fixed (byte* iidPtr = componentIidBytes)
        {
            var hr = factory.CreateInstance((IntPtr)cidPtr, (IntPtr)iidPtr, out componentPtr);
            if (hr != TResultCodes.Ok || componentPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"factory.createInstance failed for class '{ClassInfo.Name}' (HRESULT 0x{hr:X8}).");
            }
        }

        // 2. QI for IAudioProcessor on the same native object. Also try IEditController on the
        // component pointer — many SDK helpers (SingleComponentEffect, AudioEffect) implement
        // both interfaces on one object; only the two-object form requires a separate factory
        // CreateInstance below.
        var processorIid = Vst3StandardInterfaceIds.IAudioProcessor;
        var qiHr = Marshal.QueryInterface(componentPtr, in processorIid, out var processorPtr);
        if (qiHr != 0)
        {
            Marshal.Release(componentPtr);
            throw new InvalidOperationException(
                $"Class '{ClassInfo.Name}' does not implement IAudioProcessor (HRESULT 0x{qiHr:X8}).");
        }

        var controllerIid = Vst3StandardInterfaceIds.IEditController;
        Marshal.QueryInterface(componentPtr, in controllerIid, out var controllerPtrFromComponent);

        // For the single-object form the controller is this same object — probe it for IMidiMapping
        // (MIDI CC / pitch-bend / mod-wheel → parameter routing). Best-effort; built into the map later.
        if (controllerPtrFromComponent != IntPtr.Zero)
        {
            var midiMapIid = Vst3StandardInterfaceIds.IMidiMapping;
            Marshal.QueryInterface(controllerPtrFromComponent, in midiMapIid, out _midiMappingPtr);
            var unitInfoIid = Vst3StandardInterfaceIds.IUnitInfo;
            Marshal.QueryInterface(controllerPtrFromComponent, in unitInfoIid, out _unitInfoPtr);
        }

        // Also probe IConnectionPoint on the component side — best-effort; many plug-ins
        // implement it, some don't.
        var connectionIid = Vst3StandardInterfaceIds.IConnectionPoint;
        Marshal.QueryInterface(componentPtr, in connectionIid, out _componentCpPtr);

        // 3. Project component + processor (and same-object controller if we got one) onto
        // managed wrappers, then release the raw refs we own.
        _component = (IComponent)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
            componentPtr, CreateObjectFlags.UniqueInstance);
        _processor = (IAudioProcessor)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
            processorPtr, CreateObjectFlags.UniqueInstance);
        if (controllerPtrFromComponent != IntPtr.Zero)
        {
            _controller = (IEditController)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
                controllerPtrFromComponent, CreateObjectFlags.UniqueInstance);
            Marshal.Release(controllerPtrFromComponent);
        }
        Marshal.Release(componentPtr);
        Marshal.Release(processorPtr);

        // 4. Build the host CCW and hand it to initialize().
        _hostApp = new Vst3HostApplication();
        _hostUnknown = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            _hostApp, CreateComInterfaceFlags.None);
        var initHr = _component.Initialize(_hostUnknown);
        if (initHr != TResultCodes.Ok)
        {
            throw new InvalidOperationException(
                $"IPluginBase::initialize failed for class '{ClassInfo.Name}' (HRESULT 0x{initHr:X8}).");
        }
        _initialized = true;

        // 4a. Two-object controller path — when the component doesn't itself implement
        // IEditController, ask it for the controller's class id and create the controller via
        // the factory. Many synths and a few large effects use this shape.
        if (_controller is null)
        {
            ResolveSeparateController(factory);
        }
        if (_controller is null)
        {
            throw new InvalidOperationException(
                $"Class '{ClassInfo.Name}' did not expose an IEditController (neither same-object QI nor IComponent::getControllerClassId yielded one).");
        }

        // 4b. Hand the controller a host component handler. Plug-ins frequently use the handler
        // pointer's presence as a "host is ready" signal — without it some plug-ins leave their
        // parameter mirror and state machinery half-initialised.
        //
        // CRITICAL: GetOrCreateComInterfaceForObject returns the CCW's *IUnknown identity*, whose
        // vtable is a bare QueryInterface/AddRef/Release. setComponentHandler stores the pointer
        // verbatim as an IComponentHandler* (the SDK only QIs it for IComponentHandler2), so the
        // plug-in's editor calls beginEdit/performEdit (vtable slots 3/4) straight on it. Handing
        // over the identity makes those calls read past the 3-slot IUnknown vtable into adjacent
        // ComWrappers memory and crash. We must QI the identity for IComponentHandler and pass that
        // interface dispatch — same pattern we already use for the IBStream / IParameterChanges CCWs.
        // PerformEdit edits (the user turning a knob in the plug-in's own editor) route through
        // _liveParamChanges — the same lock-free queue MIDI-controller routing uses — so the
        // audio thread picks them up during its per-block IParameterChanges drain. Without this
        // path, SDK-helper-based plug-ins (Arturia, etc.) see editor edits silently dropped:
        // their DSP only reads parameters via inputParameterChanges. JUCE-wrapped plug-ins
        // pump their own UI→DSP edits internally and look unaffected.
        var componentHandler = new Vst3ComponentHandler(
            (id, value) =>
            {
                var clamped = value < 0 ? 0 : value > 1 ? 1 : value;
                _liveParamChanges.Enqueue((ComputeTargetSample(Stopwatch.GetTimestamp()), id, clamped));
            },
            OnRestartComponent);
        var handlerIdentity = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            componentHandler, CreateComInterfaceFlags.None);
        try
        {
            var ichIid = Vst3StandardInterfaceIds.IComponentHandler;
            var handlerQiHr = Marshal.QueryInterface(handlerIdentity, in ichIid, out _componentHandlerUnknown);
            if (handlerQiHr != 0 || _componentHandlerUnknown == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to QI host-side IComponentHandler (HRESULT 0x{handlerQiHr:X8}).");
            }
        }
        finally
        {
            Marshal.Release(handlerIdentity);
        }
        _controller.SetComponentHandler(_componentHandlerUnknown);

        // 4c. For the two-object form, connect both halves via IConnectionPoint first — some
        // plug-ins' setComponentState calls expect the message channel to already exist. Matches
        // the SDK's `PlugProvider::initializeAndConnectComponentController` ordering.
        if (_hasSeparateController)
        {
            TryConnectComponentAndController();
        }

        // 4d. Sync the component's startup state into the controller so its parameter mirror is
        // consistent before we ever call getParameterCount. Skipping this step leaves many
        // plug-ins reporting stale or default parameter values from the controller side.
        SyncComponentStateToController();

        // 5. Validate the bus topology. Effects need at least one audio input and one audio output
        // bus. Instruments (VSTis) have no audio input but must expose an audio output bus and an
        // event input bus to receive notes.
        if (_component.GetBusCount(MediaType.Audio, BusDirection.Output) < 1)
        {
            throw new InvalidOperationException(
                $"Class '{ClassInfo.Name}' does not expose an audio output bus.");
        }
        if (!_isInstrument && _component.GetBusCount(MediaType.Audio, BusDirection.Input) < 1)
        {
            throw new InvalidOperationException(
                $"Effect class '{ClassInfo.Name}' does not expose an audio input bus.");
        }
        if (_isInstrument && _component.GetBusCount(MediaType.Event, BusDirection.Input) < 1)
        {
            throw new InvalidOperationException(
                $"Instrument class '{ClassInfo.Name}' does not expose an event input bus.");
        }

        // 6. Negotiate bus arrangement. Try stereo first; if the plug-in refuses, fall back to
        // its declared default per the SDK contract (host should re-issue setBusArrangements with
        // the plug-in's chosen layout). Only mono and stereo are supported in this phase — wider
        // arrangements are rejected.
        NegotiateBusArrangement();

        // 7. Setup processing — must happen before ActivateBus per the SDK state machine.
        var setup = new ProcessSetup
        {
            ProcessMode = (int)ProcessMode.Offline,
            SymbolicSampleSize = (int)SymbolicSampleSize.Sample32,
            MaxSamplesPerBlock = MaxBlockSize,
            SampleRate = SampleRate,
        };
        var setupHr = _processor.SetupProcessing(ref setup);
        if (setupHr != TResultCodes.Ok)
        {
            throw new InvalidOperationException(
                $"IAudioProcessor::setupProcessing failed for class '{ClassInfo.Name}' (HRESULT 0x{setupHr:X8}).");
        }

        // 8. Activate bus 0 (the one we feed audio through) and explicitly deactivate the rest.
        // Plug-ins with sidechain or aux buses report numBuses > 1; without an explicit state for
        // those buses, behaviour is plug-in-specific. We pin every bus to a known state so the
        // matching all-zero AudioBusBuffers entries we send in ProcessData are well-defined.
        // Instruments may have no audio input bus at all (_inputBusCount == 0) — only activate
        // input bus 0 when one actually exists.
        if (_inputBusCount > 0)
        {
            var actInHr = _component.ActivateBus(MediaType.Audio, BusDirection.Input, 0, 1);
            if (actInHr != TResultCodes.Ok)
            {
                throw new InvalidOperationException(
                    $"ActivateBus(input) failed for class '{ClassInfo.Name}' (HRESULT 0x{actInHr:X8}).");
            }
        }
        var actOutHr = _component.ActivateBus(MediaType.Audio, BusDirection.Output, 0, 1);
        if (actOutHr != TResultCodes.Ok)
        {
            throw new InvalidOperationException(
                $"ActivateBus(output) failed for class '{ClassInfo.Name}' (HRESULT 0x{actOutHr:X8}).");
        }
        for (var i = 1; i < _inputBusCount; i++)
        {
            _component.ActivateBus(MediaType.Audio, BusDirection.Input, i, 0);
        }
        for (var i = 1; i < _outputBusCount; i++)
        {
            _component.ActivateBus(MediaType.Audio, BusDirection.Output, i, 0);
        }

        // 8a. Instruments: activate the event input bus (bus 0) so the plug-in receives notes.
        if (_isInstrument)
        {
            var actEvHr = _component.ActivateBus(MediaType.Event, BusDirection.Input, 0, 1);
            _eventInputActive = actEvHr == TResultCodes.Ok;
        }

        // 9. Allocate the per-channel native buffers + pointer arrays + AudioBusBuffers structs.
        AllocateNativeBuffers();

        // 10. Activate and start processing. kNotImplemented is a documented-valid response from
        // both SetActive and SetProcessing — many plug-ins (iZotope's whole catalogue, for
        // instance) leave SetProcessing as the SDK helper's default no-op that returns
        // kNotImplemented. Treat anything but a hard failure (kInvalidArgument /
        // kInternalError-shaped errors) as success.
        var setActiveHr = _component.SetActive(1);
        if (setActiveHr != TResultCodes.Ok && setActiveHr != TResultCodes.NotImplemented)
        {
            throw new InvalidOperationException(
                $"SetActive(true) failed for class '{ClassInfo.Name}' (HRESULT 0x{setActiveHr:X8}).");
        }
        _active = true;
        var setProcessingHr = _processor.SetProcessing(1);
        if (setProcessingHr != TResultCodes.Ok && setProcessingHr != TResultCodes.NotImplemented)
        {
            throw new InvalidOperationException(
                $"SetProcessing(true) failed for class '{ClassInfo.Name}' (HRESULT 0x{setProcessingHr:X8}).");
        }
        _processing = true;

        // 11. Cache latency and tail length now that the plug-in is in Setup Done.
        _latencySamples = (int)_processor.GetLatencySamples();
        TailSamples = _processor.GetTailSamples();

        // 12. Build the parameter collection by walking the controller, and stand up the
        // host-side IParameterChanges that Process() drains pending writes into.
        Parameters = BuildParameterCollection();
        BuildMidiControllerMap();
        CacheProgramChangeParameter();
        BuildUnitModel();
        _inputChanges = new Vst3HostParameterChanges();
        var inputChangesUnk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            _inputChanges, CreateComInterfaceFlags.None);
        try
        {
            var ipcIid = Vst3StandardInterfaceIds.IParameterChanges;
            var qiPcHr = Marshal.QueryInterface(inputChangesUnk, in ipcIid, out _inputChangesPtr);
            if (qiPcHr != 0 || _inputChangesPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to QI host-side IParameterChanges (HRESULT 0x{qiPcHr:X8}).");
            }
        }
        finally
        {
            Marshal.Release(inputChangesUnk);
        }

        // 13. Instruments: stand up the host-side IEventList that Process feeds scheduled notes
        // through (ProcessData.InputEvents).
        if (_isInstrument)
        {
            _inputEvents = new Vst3HostEventList();
            var inputEventsUnk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
                _inputEvents, CreateComInterfaceFlags.None);
            try
            {
                var ielIid = Vst3StandardInterfaceIds.IEventList;
                var qiElHr = Marshal.QueryInterface(inputEventsUnk, in ielIid, out _inputEventsPtr);
                if (qiElHr != 0 || _inputEventsPtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        $"Failed to QI host-side IEventList (HRESULT 0x{qiElHr:X8}).");
                }
            }
            finally
            {
                Marshal.Release(inputEventsUnk);
            }
        }
    }

    /// <summary>
    /// Negotiates the input/output speaker arrangement with the plug-in, following the SDK pattern:
    /// query every bus's declared default, then try to override bus 0 (the main bus) to stereo. If
    /// the plug-in rejects, fall back to the plug-in's full default arrangement. <c>numIns</c> and
    /// <c>numOuts</c> always match <c>IComponent::getBusCount</c> — some plug-ins (NI Supercharger
    /// GT, TAL-Dub-X) reject any <c>setBusArrangements</c> call where the bus count doesn't match.
    /// Only mono and stereo arrangements on bus 0 are supported in this phase.
    /// </summary>
    private void NegotiateBusArrangement()
    {
        var numIn = _component!.GetBusCount(MediaType.Audio, BusDirection.Input);
        var numOut = _component.GetBusCount(MediaType.Audio, BusDirection.Output);

        ulong* inArrs = stackalloc ulong[Math.Max(1, numIn)];
        ulong* outArrs = stackalloc ulong[Math.Max(1, numOut)];
        for (var i = 0; i < numIn; i++)
        {
            ulong a = 0;
            _processor!.GetBusArrangement(BusDirection.Input, i, ref a);
            inArrs[i] = a;
        }
        for (var i = 0; i < numOut; i++)
        {
            ulong a = 0;
            _processor!.GetBusArrangement(BusDirection.Output, i, ref a);
            outArrs[i] = a;
        }

        // First try: force bus 0 to stereo (the preferred render path) but keep all other buses at
        // their plug-in-declared defaults.
        var origIn0 = numIn > 0 ? inArrs[0] : 0;
        var origOut0 = numOut > 0 ? outArrs[0] : 0;
        if (numIn > 0) inArrs[0] = SpeakerArrangements.Stereo;
        if (numOut > 0) outArrs[0] = SpeakerArrangements.Stereo;
        var hr = _processor!.SetBusArrangements(
            (IntPtr)inArrs, numIn, (IntPtr)outArrs, numOut);

        if (hr != TResultCodes.Ok)
        {
            // Plug-in refused stereo — restore the declared defaults and re-issue.
            if (numIn > 0) inArrs[0] = origIn0;
            if (numOut > 0) outArrs[0] = origOut0;
            var retryHr = _processor.SetBusArrangements(
                (IntPtr)inArrs, numIn, (IntPtr)outArrs, numOut);
            if (retryHr != TResultCodes.Ok)
            {
                throw new InvalidOperationException(
                    $"Class '{ClassInfo.Name}' rejected stereo (HRESULT 0x{hr:X8}) and also rejected its own declared arrangement (bus0 in=0x{origIn0:X16}, out=0x{origOut0:X16}, HRESULT 0x{retryHr:X8}, busCount {numIn}/{numOut}).");
            }
        }

        var bus0In = numIn > 0 ? inArrs[0] : 0;
        var bus0Out = numOut > 0 ? outArrs[0] : 0;
        var inCh = ChannelCount(bus0In);
        var outCh = ChannelCount(bus0Out);
        // Instruments legitimately have no audio input (inCh == 0); effects need 1 or 2.
        if (inCh is not (0 or 1 or 2) || outCh is not (1 or 2))
        {
            throw new InvalidOperationException(
                $"Class '{ClassInfo.Name}' negotiated to an unsupported arrangement (in=0x{bus0In:X16}/{inCh}ch, out=0x{bus0Out:X16}/{outCh}ch). Only mono and stereo on bus 0 are supported in this phase.");
        }

        _inputArrangement = bus0In;
        _outputArrangement = bus0Out;
        _inputChannels = inCh;
        _outputChannels = outCh;
        _inputBusCount = numIn;
        _outputBusCount = numOut;
    }

    /// <summary>Number of set bits in a <c>SpeakerArrangement</c> mask = number of channels.</summary>
    private static int ChannelCount(ulong arrangement) =>
        System.Numerics.BitOperations.PopCount(arrangement);

    private void ResolveSeparateController(IPluginFactory factory)
    {
        Span<byte> controllerCid = stackalloc byte[16];
        fixed (byte* cidPtr = controllerCid)
        {
            var cidHr = _component!.GetControllerClassId((IntPtr)cidPtr);
            if (cidHr != TResultCodes.Ok)
            {
                return;
            }
        }
        // Treat all-zero CID as "no controller advertised".
        var allZero = true;
        for (var i = 0; i < controllerCid.Length; i++)
        {
            if (controllerCid[i] != 0) { allZero = false; break; }
        }
        if (allZero)
        {
            return;
        }

        var iidBytes = Vst3StandardInterfaceIds.IEditController.ToByteArray(bigEndian: false);
        IntPtr controllerPtr;
        fixed (byte* cidPtr = controllerCid)
        fixed (byte* iidPtr = iidBytes)
        {
            var createHr = factory.CreateInstance((IntPtr)cidPtr, (IntPtr)iidPtr, out controllerPtr);
            if (createHr != TResultCodes.Ok || controllerPtr == IntPtr.Zero)
            {
                return;
            }
        }
        // Probe the separate controller for IConnectionPoint + IMidiMapping before releasing the ref.
        var connectionIid = Vst3StandardInterfaceIds.IConnectionPoint;
        Marshal.QueryInterface(controllerPtr, in connectionIid, out _controllerCpPtr);
        var sepMidiMapIid = Vst3StandardInterfaceIds.IMidiMapping;
        Marshal.QueryInterface(controllerPtr, in sepMidiMapIid, out _midiMappingPtr);
        var sepUnitInfoIid = Vst3StandardInterfaceIds.IUnitInfo;
        Marshal.QueryInterface(controllerPtr, in sepUnitInfoIid, out _unitInfoPtr);

        _controller = (IEditController)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
            controllerPtr, CreateObjectFlags.UniqueInstance);
        Marshal.Release(controllerPtr);
        _hasSeparateController = true;

        var ctrlInitHr = _controller.Initialize(_hostUnknown);
        if (ctrlInitHr != TResultCodes.Ok)
        {
            throw new InvalidOperationException(
                $"IEditController::initialize failed for '{ClassInfo.Name}' (HRESULT 0x{ctrlInitHr:X8}).");
        }
        _controllerInitialized = true;
    }

    private void SyncComponentStateToController()
    {
        // Spec: feed the component's getState() output to the controller's setComponentState() so
        // the controller starts with the same parameter values the DSP has cached. Plug-ins that
        // skip getState (kNotImplemented / kResultFalse) just leave the controller at defaults.
        //
        // One-object plug-ins (where the controller is the same native object as the component)
        // would round-trip their own state through this — pointless and surfaces buggy
        // setComponentState implementations in some plug-ins (which deref the stream without
        // first checking the read result code). Skip in that case.
        if (!_hasSeparateController)
        {
            return;
        }
        using var stream = new Vst3MemoryStream();
        var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            stream, CreateComInterfaceFlags.None);
        try
        {
            var iid = Vst3StandardInterfaceIds.IBStream;
            var qiHr = Marshal.QueryInterface(unk, in iid, out var streamPtr);
            if (qiHr != 0 || streamPtr == IntPtr.Zero)
            {
                return;
            }
            try
            {
                var getHr = _component!.GetState(streamPtr);
                if (getHr != TResultCodes.Ok)
                {
                    return;
                }
                // Rewind so the controller reads from the start of the blob.
                stream.Seek(0, StreamSeekMode.Set, IntPtr.Zero);
                _controller!.SetComponentState(streamPtr);
            }
            finally
            {
                Marshal.Release(streamPtr);
            }
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    private void TryConnectComponentAndController()
    {
        if (_componentCpPtr == IntPtr.Zero || _controllerCpPtr == IntPtr.Zero)
        {
            return;
        }

        // IConnectionPoint::Connect takes a native IConnectionPoint*, not an IUnknown, so we
        // call it via direct vtable dispatch on the cached raw pointers. Vtable layout:
        // 0..2 IUnknown, 3 Connect, 4 Disconnect, 5 Notify.
        unsafe
        {
            var compVt = *(IntPtr**)_componentCpPtr;
            var ctrlVt = *(IntPtr**)_controllerCpPtr;
            var compConnect = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)compVt[3];
            var ctrlConnect = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)ctrlVt[3];
            // Each side receives the OTHER side's IConnectionPoint*. Order matches the SDK
            // plugprovider: component → controller first, then controller → component.
            var hr1 = compConnect(_componentCpPtr, _controllerCpPtr);
            var hr2 = ctrlConnect(_controllerCpPtr, _componentCpPtr);
            if (hr1 == TResultCodes.Ok && hr2 == TResultCodes.Ok)
            {
                _connected = true;
            }
        }
    }

    private Vst3ParameterCollection BuildParameterCollection()
    {
        var count = _controller!.GetParameterCount();
        if (count <= 0)
        {
            return new Vst3ParameterCollection(Array.Empty<Vst3Parameter>());
        }
        var list = new List<Vst3Parameter>(count);
        for (var i = 0; i < count; i++)
        {
            var hr = _controller.GetParameterInfo(i, out var info);
            if (hr != TResultCodes.Ok)
            {
                continue;
            }
            string title;
            string shortTitle;
            string units;
            unsafe
            {
                title = ReadFixedUtf16(info.Title, 128);
                shortTitle = ReadFixedUtf16(info.ShortTitle, 128);
                units = ReadFixedUtf16(info.Units, 128);
            }
            list.Add(new Vst3Parameter(
                this,
                info.Id,
                title,
                shortTitle,
                units,
                info.StepCount,
                info.DefaultNormalizedValue,
                info.UnitId,
                (Vst3ParameterFlags)info.Flags));
        }
        return new Vst3ParameterCollection(list);
    }

    private static unsafe string ReadFixedUtf16(char* buffer, int maxChars)
    {
        var span = new ReadOnlySpan<char>(buffer, maxChars);
        var nul = span.IndexOf('\0');
        if (nul >= 0)
        {
            span = span[..nul];
        }
        return new string(span);
    }

    /// <summary>Reads the current normalised value for a parameter via the edit controller.</summary>
    internal double GetParameterNormalized(uint id)
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        return controller.GetParamNormalized(id);
    }

    /// <summary>
    /// Queues a normalised-value write to be applied at sample offset 0 of the next
    /// <see cref="Process"/> call via <c>IParameterChanges</c>. After the block, the new value
    /// is also mirrored into the controller so subsequent reads reflect it.
    /// </summary>
    internal void SetParameterNormalized(uint id, double value)
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        // Clamp defensively — the SDK doesn't forbid out-of-range, but plug-ins that hit
        // assertions in debug builds are unhelpful. Callers can still pass exact 0 / 1.
        if (value < 0) value = 0;
        else if (value > 1) value = 1;
        // Queue the write for the DSP at offset 0 of the next Process block via the lock-free
        // immediate queue (the same one the segment-driven Enqueue* path drains). Process runs on
        // the audio thread, so a plain Dictionary here would be a data race. Mirror the value into
        // the controller now, on the caller's thread, so a subsequent read sees it immediately —
        // and so the audio thread never has to touch the controller. The managed Parameters API is
        // therefore single-threaded per the VST 3 controller contract: drive it from one thread.
        _immediateParamChanges.Enqueue((id, value));
        controller.SetParamNormalized(id, value);
    }

    internal double NormalizedToPlain(uint id, double normalized)
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        return controller.NormalizedParamToPlain(id, normalized);
    }

    internal double PlainToNormalized(uint id, double plain)
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        return controller.PlainParamToNormalized(id, plain);
    }

    internal string FormatParameter(uint id, double normalized)
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        // Vst::String128 = char16[128], native expects a buffer it writes UTF-16 into.
        const int capacity = 128;
        Span<char> buffer = stackalloc char[capacity];
        buffer.Clear();
        fixed (char* bufPtr = buffer)
        {
            var hr = controller.GetParamStringByValue(id, normalized, (IntPtr)bufPtr);
            if (hr != TResultCodes.Ok)
            {
                return string.Empty;
            }
            var slice = buffer;
            var nul = slice.IndexOf('\0');
            if (nul >= 0)
            {
                slice = slice[..nul];
            }
            return new string(slice);
        }
    }

    /// <summary>
    /// Creates the plug-in's editor view (its GUI), or returns <c>null</c> when the plug-in does
    /// not provide one (<c>IEditController::createView</c> returned a null pointer).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Editor hosting is a Windows-only feature in this release; the returned
    /// <see cref="Vst3PluginView"/> is attached to an <c>HWND</c>. Call this on the host's UI
    /// (STA) thread — the view inherits that thread's affinity.
    /// </para>
    /// <para>
    /// The returned view must be disposed <b>before</b> this plug-in is disposed: the native view
    /// is owned by the controller, and releasing it after <c>terminate</c> is undefined.
    /// </para>
    /// </remarks>
    public Vst3PluginView? CreateView()
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        // ViewType::kEditor — a null-terminated ASCII C string.
        ReadOnlySpan<byte> editor = "editor\0"u8;
        IntPtr viewPtr;
        fixed (byte* namePtr = editor)
        {
            viewPtr = controller.CreateView((IntPtr)namePtr);
        }
        return viewPtr == IntPtr.Zero ? null : new Vst3PluginView(viewPtr);
    }

    /// <summary>
    /// Magic header for the byte-blob produced by <see cref="SaveState"/> — <c>"V3ST"</c>
    /// followed by an <see cref="int"/> format version.
    /// </summary>
    private static ReadOnlySpan<byte> StateMagic => "V3ST"u8;

    /// <summary>Wire-format version for the bundle written by <see cref="SaveState"/>.</summary>
    private const int StateVersion = 1;

    /// <summary>
    /// Captures the plug-in's full state — both the component (DSP) blob and the controller
    /// (UI / parameter mirror) blob — into a single self-describing byte array.
    /// </summary>
    /// <remarks>
    /// The component and controller emit their own opaque binary representations via
    /// <c>IComponent::getState</c> and <c>IEditController::getState</c>. Most plug-ins prefer
    /// these blobs round-trip together (changing parameters in one without the other can leave
    /// the two halves inconsistent); this method always saves both.
    /// </remarks>
    public byte[] SaveState()
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        var component = _component ?? throw new ObjectDisposedException(nameof(Vst3Plugin));

        var componentBlob = CaptureBlob(component.GetState);
        var controllerBlob = CaptureBlob(controller.GetState);

        var totalLen = StateMagic.Length + sizeof(int) + sizeof(int) + componentBlob.Length
            + sizeof(int) + controllerBlob.Length;
        var result = new byte[totalLen];
        var dst = result.AsSpan();
        var offset = 0;
        StateMagic.CopyTo(dst[offset..]); offset += StateMagic.Length;
        BitConverter.TryWriteBytes(dst[offset..], StateVersion); offset += sizeof(int);
        BitConverter.TryWriteBytes(dst[offset..], componentBlob.Length); offset += sizeof(int);
        componentBlob.AsSpan().CopyTo(dst[offset..]); offset += componentBlob.Length;
        BitConverter.TryWriteBytes(dst[offset..], controllerBlob.Length); offset += sizeof(int);
        controllerBlob.AsSpan().CopyTo(dst[offset..]);
        return result;
    }

    /// <summary>
    /// Restores a state blob previously produced by <see cref="SaveState"/>. The component blob
    /// is applied first (so the DSP picks up its cached values), then the controller blob (so
    /// the parameter mirror matches).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// When the blob does not start with the <c>V3ST</c> magic, declares an unsupported version,
    /// or is truncated.
    /// </exception>
    public void LoadState(ReadOnlySpan<byte> stateBytes)
    {
        if (_controller is null || _component is null)
        {
            throw new ObjectDisposedException(nameof(Vst3Plugin));
        }

        var header = StateMagic.Length + sizeof(int) + sizeof(int);
        if (stateBytes.Length < header)
        {
            throw new ArgumentException("State blob is too short to contain a valid header.", nameof(stateBytes));
        }
        if (!stateBytes[..StateMagic.Length].SequenceEqual(StateMagic))
        {
            throw new ArgumentException("State blob is missing the 'V3ST' magic.", nameof(stateBytes));
        }
        var offset = StateMagic.Length;
        var version = BitConverter.ToInt32(stateBytes[offset..]); offset += sizeof(int);
        if (version != StateVersion)
        {
            throw new ArgumentException(
                $"State blob declares version {version}; this build only supports v{StateVersion}.",
                nameof(stateBytes));
        }
        var compLen = BitConverter.ToInt32(stateBytes[offset..]); offset += sizeof(int);
        if (compLen < 0 || offset + compLen + sizeof(int) > stateBytes.Length)
        {
            throw new ArgumentException("State blob is truncated (component section).", nameof(stateBytes));
        }
        var componentBytes = stateBytes.Slice(offset, compLen).ToArray();
        offset += compLen;
        var ctrlLen = BitConverter.ToInt32(stateBytes[offset..]); offset += sizeof(int);
        if (ctrlLen < 0 || offset + ctrlLen > stateBytes.Length)
        {
            throw new ArgumentException("State blob is truncated (controller section).", nameof(stateBytes));
        }
        var controllerBytes = stateBytes.Slice(offset, ctrlLen).ToArray();

        ApplyComponentAndControllerState(componentBytes, controllerBytes);
    }

    /// <summary>
    /// Applies a component (DSP) state blob and an optional controller state blob to the live
    /// plug-in, under the SDK-mandated "not processing" bracket. Shared by <see cref="LoadState"/>
    /// and <see cref="LoadPreset(Stream)"/>.
    /// </summary>
    /// <param name="componentBytes">The component state to apply via <c>IComponent::setState</c>.</param>
    /// <param name="controllerBytes">
    /// The controller state to apply via <c>IEditController::setState</c>, or null to skip it (a
    /// <c>.vstpreset</c> need not carry a controller chunk).
    /// </param>
    private void ApplyComponentAndControllerState(byte[] componentBytes, byte[]? controllerBytes)
    {
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        var component = _component ?? throw new ObjectDisposedException(nameof(Vst3Plugin));

        // Stop processing for the setState calls. The SDK forbids calling setState while a process()
        // is in flight, so we toggle processing off and back on. We deliberately do NOT toggle
        // setActive — Phase 5 step 3 found that the full setProcessing(false)+setActive(false)
        // bracket is harmful to some plug-ins (Crystalline, CHOWTapeModel discard the loaded
        // state). The narrow bracket gives plug-ins a clean "not processing" window without going
        // through the bus-reconfig path that setActive(false) triggers.
        var resumeProcessing = false;
        if (_processing && _processor is not null)
        {
            _processor.SetProcessing(0);
            _processing = false;
            resumeProcessing = true;
        }

        try
        {
            // Apply the three halves in the SDK-documented order: component DSP state → controller's
            // mirror of the component state (so its parameter cache catches up) → controller's own state.
            // The setComponentState step is for the two-object form only — for one-object plug-ins it
            // would feed the component's own blob back to itself through a different vtable slot, and
            // some JUCE wrappers crash on that (see SyncComponentStateToController for the matching skip).
            ApplyBlob(component.SetState, componentBytes);
            if (_hasSeparateController)
            {
                ApplyBlob(controller.SetComponentState, componentBytes);
            }
            if (controllerBytes is not null)
            {
                ApplyBlob(controller.SetState, controllerBytes);
            }

            // Drop any queued parameter writes — the loaded state supersedes them.
            while (_immediateParamChanges.TryDequeue(out _)) { }
        }
        finally
        {
            if (resumeProcessing && _processor is not null)
            {
                var hr = _processor.SetProcessing(1);
                // kNotImplemented is documented-valid for SetProcessing — match Initialise's tolerance.
                if (hr == TResultCodes.Ok || hr == TResultCodes.NotImplemented)
                {
                    _processing = true;
                }
            }
        }
    }

    /// <summary>
    /// Saves the plug-in's current state to a Steinberg <c>.vstpreset</c> file at
    /// <paramref name="filePath"/> (overwriting any existing file).
    /// </summary>
    /// <remarks>
    /// Captures both the component (DSP) and controller (parameter) state, the same pair persisted by
    /// <see cref="SaveState"/>, but in the portable <c>.vstpreset</c> container so the file can be
    /// loaded by other VST 3 hosts (and vice-versa via <see cref="LoadPreset(string)"/>).
    /// </remarks>
    public void SavePreset(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        using var fs = File.Create(filePath);
        SavePreset(fs);
    }

    /// <summary>Saves the plug-in's current state as a <c>.vstpreset</c> to the given seekable stream.</summary>
    public void SavePreset(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var controller = _controller ?? throw new ObjectDisposedException(nameof(Vst3Plugin));
        var component = _component ?? throw new ObjectDisposedException(nameof(Vst3Plugin));

        var componentBlob = CaptureBlob(component.GetState);
        var controllerBlob = CaptureBlob(controller.GetState);
        Vst3Preset.Write(stream, ClassInfo.ClassId, componentBlob, controllerBlob);
    }

    /// <summary>
    /// Loads a Steinberg <c>.vstpreset</c> file from <paramref name="filePath"/> and applies it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The preset belongs to a different plug-in class than this one.
    /// </exception>
    /// <exception cref="InvalidDataException">The file is not a well-formed <c>.vstpreset</c>.</exception>
    public void LoadPreset(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        using var fs = File.OpenRead(filePath);
        LoadPreset(fs);
    }

    /// <summary>Loads a <c>.vstpreset</c> from the given seekable stream and applies it.</summary>
    /// <exception cref="InvalidOperationException">
    /// The preset belongs to a different plug-in class than this one.
    /// </exception>
    /// <exception cref="InvalidDataException">The stream is not a well-formed <c>.vstpreset</c>.</exception>
    public void LoadPreset(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (_controller is null || _component is null)
        {
            throw new ObjectDisposedException(nameof(Vst3Plugin));
        }

        var contents = Vst3Preset.Read(stream);
        if (!string.Equals(contents.ClassId, ClassInfo.ClassId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Preset belongs to class {contents.ClassId}, but this plug-in is " +
                $"{ClassInfo.ClassId} ({ClassInfo.Name}).");
        }

        ApplyComponentAndControllerState(contents.ComponentState, contents.ControllerState);
    }

    private static byte[] CaptureBlob(Func<IntPtr, int> writeTo)
    {
        using var stream = new Vst3MemoryStream();
        var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            stream, CreateComInterfaceFlags.None);
        try
        {
            var iid = Vst3StandardInterfaceIds.IBStream;
            var qiHr = Marshal.QueryInterface(unk, in iid, out var streamPtr);
            if (qiHr != 0 || streamPtr == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }
            try
            {
                var hr = writeTo(streamPtr);
                if (hr != TResultCodes.Ok && hr != TResultCodes.False)
                {
                    // Plug-ins that don't implement persistence (kNotImplemented) just save empty.
                    return Array.Empty<byte>();
                }
                return stream.ToArray();
            }
            finally
            {
                Marshal.Release(streamPtr);
            }
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    private static void ApplyBlob(Func<IntPtr, int> readFrom, byte[] bytes)
    {
        using var stream = new Vst3MemoryStream(bytes);
        var unk = Vst3ComWrappers.Instance.GetOrCreateComInterfaceForObject(
            stream, CreateComInterfaceFlags.None);
        try
        {
            var iid = Vst3StandardInterfaceIds.IBStream;
            var qiHr = Marshal.QueryInterface(unk, in iid, out var streamPtr);
            if (qiHr != 0 || streamPtr == IntPtr.Zero)
            {
                return;
            }
            try
            {
                readFrom(streamPtr);
            }
            finally
            {
                Marshal.Release(streamPtr);
            }
        }
        finally
        {
            Marshal.Release(unk);
        }
    }

    private void AllocateNativeBuffers()
    {
        var blockBytes = (nuint)(MaxBlockSize * sizeof(float));

        _inputBuffers = (float**)NativeMemory.AllocZeroed((nuint)(_inputChannels * sizeof(float*)));
        _inputChannelPtrs = (IntPtr*)NativeMemory.AllocZeroed((nuint)(_inputChannels * sizeof(IntPtr)));
        for (var c = 0; c < _inputChannels; c++)
        {
            _inputBuffers[c] = (float*)NativeMemory.AllocZeroed(blockBytes);
            _inputChannelPtrs[c] = (IntPtr)_inputBuffers[c];
        }

        _outputBuffers = (float**)NativeMemory.AllocZeroed((nuint)(_outputChannels * sizeof(float*)));
        _outputChannelPtrs = (IntPtr*)NativeMemory.AllocZeroed((nuint)(_outputChannels * sizeof(IntPtr)));
        for (var c = 0; c < _outputChannels; c++)
        {
            _outputBuffers[c] = (float*)NativeMemory.AllocZeroed(blockBytes);
            _outputChannelPtrs[c] = (IntPtr)_outputBuffers[c];
        }

        // _inputBus / _outputBus are arrays sized to the plug-in's declared bus count, not single
        // structs. The Process call passes (Inputs, NumInputs) where NumInputs must equal
        // getBusCount(). Bus 0 carries the audio we actually feed; the remaining buses we
        // deactivated above stay all-zero (NumChannels=0, ChannelBuffers=null, SilenceFlags=0),
        // which is the documented shape for an unused bus.
        // Instruments have no audio input bus (_inputBusCount == 0); allocate a single zeroed slot
        // so Inputs is non-null, but only populate bus 0 when there actually is an input bus.
        _inputBus = (AudioBusBuffers*)NativeMemory.AllocZeroed((nuint)(Math.Max(1, _inputBusCount) * sizeof(AudioBusBuffers)));
        if (_inputBusCount > 0)
        {
            _inputBus[0].NumChannels = _inputChannels;
            _inputBus[0].SilenceFlags = 0;
            _inputBus[0].ChannelBuffers = (IntPtr)_inputChannelPtrs;
        }

        _outputBus = (AudioBusBuffers*)NativeMemory.AllocZeroed((nuint)(_outputBusCount * sizeof(AudioBusBuffers)));
        _outputBus[0].NumChannels = _outputChannels;
        _outputBus[0].SilenceFlags = 0;
        _outputBus[0].ChannelBuffers = (IntPtr)_outputChannelPtrs;
    }

    /// <summary>
    /// Processes one block of audio. Buffers are interleaved per the negotiated channel count
    /// (<see cref="InputChannelCount"/> floats per input frame, <see cref="OutputChannelCount"/>
    /// floats per output frame).
    /// </summary>
    /// <param name="interleavedInput">Input audio, <c>numSamples * InputChannelCount</c> floats.</param>
    /// <param name="interleavedOutput">Output buffer, <c>numSamples * OutputChannelCount</c> floats.</param>
    /// <param name="numSamples">Number of audio frames to process. Must be ≤ <see cref="MaxBlockSize"/>.</param>
    /// <remarks>
    /// This is the audio-thread entry point and is normally called from a render callback. It
    /// <b>can throw</b> — both for an invalid request (see the exceptions below) and if the plug-in's
    /// <c>IAudioProcessor::process</c> returns a failure code, which indicates a broken plug-in.
    /// Because an exception escaping a render callback tears down the audio stream, a host that wants
    /// to keep playing through a misbehaving plug-in should catch around this call and substitute
    /// silence (the built-in <see cref="Vst3EffectSampleProvider"/> / <see cref="Vst3InstrumentSampleProvider"/>
    /// wrappers do not — they let the failure propagate so it surfaces rather than hiding silently).
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The plug-in has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="numSamples"/> is negative or exceeds <see cref="MaxBlockSize"/>.</exception>
    /// <exception cref="ArgumentException">An input or output buffer is too small for the channel count.</exception>
    /// <exception cref="InvalidOperationException"><c>IAudioProcessor::process</c> returned a failure HRESULT.</exception>
    public void Process(
        ReadOnlySpan<float> interleavedInput,
        Span<float> interleavedOutput,
        int numSamples)
    {
        if (_processor is null)
        {
            throw new ObjectDisposedException(nameof(Vst3Plugin));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(numSamples);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(numSamples, MaxBlockSize);
        if (interleavedInput.Length < numSamples * _inputChannels)
        {
            throw new ArgumentException(
                $"Input buffer too small for numSamples * {_inputChannels} floats.", nameof(interleavedInput));
        }
        if (interleavedOutput.Length < numSamples * _outputChannels)
        {
            throw new ArgumentException(
                $"Output buffer too small for numSamples * {_outputChannels} floats.", nameof(interleavedOutput));
        }

        // De-interleave input into per-channel buffers the SDK expects.
        for (var c = 0; c < _inputChannels; c++)
        {
            var dst = new Span<float>(_inputBuffers[c], numSamples);
            for (var i = 0; i < numSamples; i++)
            {
                dst[i] = interleavedInput[(i * _inputChannels) + c];
            }
        }

        // Clear output buffers — some plug-ins skip writing during inactivity / silence and we
        // don't want stale data leaking through.
        for (var c = 0; c < _outputChannels; c++)
        {
            new Span<float>(_outputBuffers[c], numSamples).Clear();
        }

        // Snapshot the block-start clock: producer-side Send* methods read this under _clockLock
        // to compute target sample positions for sample-accurate event/CC timing.
        lock (_clockLock)
        {
            _blockStartSample = _samplePosition;
            _blockStartTimestampTicks = Stopwatch.GetTimestamp();
        }

        var blockEnd = _samplePosition + numSamples;

        // Drain any queued parameter writes into the host-side IParameterChanges. Pending writes
        // and live MIDI-controller changes both target sample offsets within the block; entries
        // past blockEnd stay in _pendingLiveParamChanges for next block.
        var inputChangesPtr = IntPtr.Zero;
        DrainLiveParamChangesIntoPending();
        if ((HasPendingParamChangesInBlock(blockEnd) || !_immediateParamChanges.IsEmpty)
            && _inputChanges is not null)
        {
            _inputChanges.BeginBlock();
            // Offset-0 writes: programmatic Parameters-API sets and segment-driven Enqueue*
            // controller changes both land at the start of this block, in arrival order.
            while (_immediateParamChanges.TryDequeue(out var immediate))
            {
                _inputChanges.AcquireQueue(immediate.Id).Append(0, immediate.Value);
            }
            DispatchPendingParamChanges(blockEnd, numSamples);
            inputChangesPtr = _inputChangesPtr;
        }

        // Instrument event input: drain the notes scheduled for this block into the host event list
        // (converting absolute sample times to block-relative offsets) and supply a process context.
        // Effects skip all of this — InputEvents / ProcessContext stay null, exactly as before.
        var inputEventsPtr = IntPtr.Zero;
        ProcessContext ctx = default;
        if (_isInstrument && _inputEvents is not null)
        {
            _inputEvents.Clear();
            // Segment-driven notes (Enqueue*) fire at offset 0 of this block, added ahead of any
            // scheduled/live events so the event list leads with offset 0.
            while (_immediateEvents.TryDequeue(out var immediate))
            {
                immediate.SampleOffset = 0;
                EmitInputEvent(immediate);
            }
            if (!_scheduleSorted)
            {
                _scheduledEvents.Sort(static (a, b) => a.SampleTime.CompareTo(b.SampleTime));
                _scheduleSorted = true;
                _scheduleCursor = 0;
            }
            while (_scheduleCursor < _scheduledEvents.Count
                && _scheduledEvents[_scheduleCursor].SampleTime < blockEnd)
            {
                var scheduled = _scheduledEvents[_scheduleCursor];
                var ev = scheduled.NativeEvent;
                var offset = scheduled.SampleTime - _samplePosition;
                ev.SampleOffset = (int)(offset < 0 ? 0 : offset);
                EmitInputEvent(ev);
                _scheduleCursor++;
            }
            // Live notes (from a MIDI callback on another thread) — sample-accurate dispatch by
            // target sample; events past blockEnd stay in _pendingLiveEvents for the next block.
            DrainLiveEventsIntoPending();
            DispatchPendingLiveEvents(blockEnd, numSamples);
            inputEventsPtr = _inputEventsPtr;
            ctx = BuildProcessContext(_samplePosition);
        }

        var data = new ProcessData
        {
            ProcessMode = (int)ProcessMode.Offline,
            SymbolicSampleSize = (int)SymbolicSampleSize.Sample32,
            NumSamples = numSamples,
            NumInputs = _inputBusCount,
            NumOutputs = _outputBusCount,
            Inputs = (IntPtr)_inputBus,
            Outputs = (IntPtr)_outputBus,
            InputParameterChanges = inputChangesPtr,
            OutputParameterChanges = IntPtr.Zero,
            InputEvents = inputEventsPtr,
            OutputEvents = IntPtr.Zero,
            ProcessContext = _isInstrument ? (IntPtr)(&ctx) : IntPtr.Zero,
        };

        var hr = _processor.Process(ref data);
        // The plug-in has read any SysEx (Data) event bytes synchronously during process(); free their
        // unmanaged buffers now, whether or not the call succeeded.
        if (_sysexBuffersThisBlock.Count > 0) FreeSysExBuffers();
        if (hr != TResultCodes.Ok)
        {
            throw new InvalidOperationException(
                $"IAudioProcessor::process failed (HRESULT 0x{hr:X8}).");
        }

        // Capture the plug-in's report of which output channels are silent this block. Bit N set =
        // channel N silent. Surfaced via LastOutputSilenceFlags as a diagnostic only — the tail
        // detector in Vst3EffectSampleProvider gates purely on output RMS, not this flag, because
        // some plug-ins never set it.
        _lastOutputSilenceFlags = _outputBus[0].SilenceFlags;

        // Re-interleave per-channel outputs back into the caller's buffer.
        for (var c = 0; c < _outputChannels; c++)
        {
            var src = new ReadOnlySpan<float>(_outputBuffers[c], numSamples);
            for (var i = 0; i < numSamples; i++)
            {
                interleavedOutput[(i * _outputChannels) + c] = src[i];
            }
        }

        // Advance the instrument event clock (unused by effects, which never schedule events).
        _samplePosition += numSamples;
    }

    /// <summary>
    /// <c>true</c> when this plug-in is an instrument (VSTi) — it has an event input bus and
    /// generates audio from scheduled notes rather than processing an audio input.
    /// </summary>
    public bool IsInstrument => _isInstrument;

    /// <summary>
    /// Bitfield of the most recent block's per-channel output-silence flags as set by the plug-in
    /// (bit <c>N</c> = output channel <c>N</c> is silent). VST 3 plug-ins write this on the output
    /// bus to let the host skip dead channels; the SDK helper does this consistently, plain
    /// <c>AudioEffect</c>-derived plug-ins sometimes do not. Exposed as a diagnostic only — the
    /// built-in tail detector in <see cref="Vst3EffectSampleProvider"/> does <b>not</b> consult it
    /// (it gates on output RMS, since some plug-ins never set the flag). Treat it as an optional hint
    /// if you implement your own tail logic.
    /// </summary>
    public ulong LastOutputSilenceFlags => _lastOutputSilenceFlags;

    /// <summary>
    /// Schedules a note-on at an absolute position on the plug-in's sample timeline (the clock
    /// <see cref="Process"/> advances by <c>numSamples</c> each block). The event is delivered in
    /// the block whose range contains <paramref name="sampleTime"/>.
    /// </summary>
    /// <param name="sampleTime">Absolute sample position at which the note starts.</param>
    /// <param name="pitch">MIDI note number (0–127).</param>
    /// <param name="velocity">Normalised velocity in [0, 1].</param>
    /// <param name="channel">Event-bus channel (0-based).</param>
    /// <exception cref="InvalidOperationException">The plug-in is not an instrument.</exception>
    public void ScheduleNoteOn(long sampleTime, int pitch, float velocity, int channel = 0)
        => ScheduleNote(sampleTime, EventType.NoteOn, channel, pitch, velocity);

    /// <summary>Schedules a note-off at an absolute sample position. See <see cref="ScheduleNoteOn"/>.</summary>
    public void ScheduleNoteOff(long sampleTime, int pitch, float velocity = 0f, int channel = 0)
        => ScheduleNote(sampleTime, EventType.NoteOff, channel, pitch, velocity);

    /// <summary>Clears all scheduled events and resets the sample clock to zero.</summary>
    public void ResetEventSchedule()
    {
        _scheduledEvents.Clear();
        _scheduleCursor = 0;
        _scheduleSorted = true;
        _samplePosition = 0;
    }

    /// <summary>
    /// Sends a note-on to be delivered at the start of the next <see cref="Process"/> block. Safe to
    /// call from any thread (e.g. a MIDI input callback) — the event is queued and consumed on the
    /// audio thread. This is the realtime/live counterpart to <see cref="ScheduleNoteOn"/>.
    /// </summary>
    /// <param name="pitch">MIDI note number (0–127).</param>
    /// <param name="velocity">Normalised velocity in [0, 1].</param>
    /// <param name="channel">Event-bus channel (0-based).</param>
    /// <exception cref="InvalidOperationException">The plug-in is not an instrument.</exception>
    /// <param name="arrivalTicks">
    /// Optional <see cref="Stopwatch.GetTimestamp"/> tick of when the MIDI event arrived. When
    /// supplied (or left at the default 0, in which case the current timestamp is captured
    /// inside), the event is dispatched at the matching sub-block sample offset on the next
    /// <see cref="Process"/> call — sample-accurate timing rather than always firing at sample 0
    /// of the next block. Pass a non-zero value when you want to use a timestamp captured
    /// earlier than the call to <see cref="SendNoteOn"/> (e.g. inside a MIDI callback that does
    /// other work first).
    /// </param>
    public void SendNoteOn(int pitch, float velocity, int channel = 0, long arrivalTicks = 0)
    {
        EnsureInstrument();
        _activeNotes[(channel << 8) | (pitch & 0xFF)] = 1;
        var target = ComputeTargetSample(arrivalTicks == 0 ? Stopwatch.GetTimestamp() : arrivalTicks);
        _liveEvents.Enqueue((target, BuildNoteEvent(EventType.NoteOn, channel, pitch, velocity)));
    }

    /// <summary>Sends a note-off for the next block from any thread. See <see cref="SendNoteOn"/>.</summary>
    public void SendNoteOff(int pitch, float velocity = 0f, int channel = 0, long arrivalTicks = 0)
    {
        EnsureInstrument();
        _activeNotes.TryRemove((channel << 8) | (pitch & 0xFF), out _);
        var target = ComputeTargetSample(arrivalTicks == 0 ? Stopwatch.GetTimestamp() : arrivalTicks);
        _liveEvents.Enqueue((target, BuildNoteEvent(EventType.NoteOff, channel, pitch, velocity)));
    }

    /// <summary>
    /// Sends a note-off for every note currently sounding (a "panic"). Safe to call from any thread.
    /// Use on stop, or to clear a stuck voice when a note-off was missed. Panic notes fire at
    /// offset 0 of the next block — sample-accurate timing isn't relevant for an emergency stop.
    /// </summary>
    public void AllNotesOff()
    {
        EnsureInstrument();
        // target = 0 → drain logic computes a negative offset and clamps to 0 (fire immediately).
        foreach (var key in _activeNotes.Keys)
        {
            _liveEvents.Enqueue((0, BuildNoteEvent(EventType.NoteOff, key >> 8, key & 0xFF, 0f)));
        }
        _activeNotes.Clear();
    }

    /// <summary>
    /// <c>true</c> when the plug-in advertises MIDI-controller → parameter assignments (via
    /// <c>IMidiMapping</c>), i.e. <see cref="SendControlChange"/> can route at least one controller.
    /// </summary>
    public bool SupportsMidiControllers => _midiCcToParam.Count > 0;

    /// <summary>
    /// Routes a MIDI control change (CC 0–127) to its assigned parameter and queues it for the next
    /// block. Safe to call from any thread. Returns <c>false</c> if the plug-in doesn't map that CC.
    /// </summary>
    /// <param name="controllerNumber">MIDI controller number (0–127), e.g. 1 = mod wheel, 64 = sustain.</param>
    /// <param name="normalizedValue">The value normalised to [0, 1] (e.g. raw CC value / 127).</param>
    /// <param name="arrivalTicks">Optional <see cref="Stopwatch.GetTimestamp"/> tick of when the
    /// controller change arrived (e.g. inside a MIDI callback). When 0 / unspecified the current
    /// timestamp is captured inside. See <see cref="SendNoteOn"/> for sample-accurate timing details.</param>
    public bool SendControlChange(int controllerNumber, double normalizedValue, long arrivalTicks = 0)
        => RouteController((short)controllerNumber, normalizedValue, arrivalTicks);

    /// <summary>Routes pitch-bend (0 = full down, 0.5 = centre, 1 = full up). See <see cref="SendControlChange"/>.</summary>
    public bool SendPitchBend(double normalizedValue, long arrivalTicks = 0)
        => RouteController(Vst3ControllerNumbers.PitchBend, normalizedValue, arrivalTicks);

    /// <summary>Routes channel pressure / aftertouch. See <see cref="SendControlChange"/>.</summary>
    public bool SendChannelPressure(double normalizedValue, long arrivalTicks = 0)
        => RouteController(Vst3ControllerNumbers.AfterTouch, normalizedValue, arrivalTicks);

    /// <summary>
    /// <c>true</c> when the plug-in exposes a program-change parameter (one flagged
    /// <c>IsProgramChange</c>), i.e. <see cref="SendProgramChange"/> / <see cref="EnqueueProgramChange"/>
    /// can select a program.
    /// </summary>
    public bool SupportsProgramChange => _hasProgramChange;

    /// <summary>
    /// Selects a program by MIDI program number, queued for the next block. VST 3 has no program-change
    /// event — this drives the plug-in's program-list parameter (flagged <c>IsProgramChange</c>) to the
    /// matching normalised value. Safe to call from any thread. Returns <c>false</c> if the plug-in has no
    /// such parameter. See <see cref="SendNoteOn"/> for the <paramref name="arrivalTicks"/> timing.
    /// </summary>
    /// <param name="program">MIDI program number (0–127).</param>
    /// <param name="arrivalTicks">Optional <see cref="Stopwatch.GetTimestamp"/> arrival tick; 0 captures it inside.</param>
    public bool SendProgramChange(int program, long arrivalTicks = 0)
    {
        if (!_hasProgramChange) return false;
        var target = ComputeTargetSample(arrivalTicks == 0 ? Stopwatch.GetTimestamp() : arrivalTicks);
        _liveParamChanges.Enqueue((target, _programChangeParamId, NormalizedProgram(program)));
        return true;
    }

    /// <summary>
    /// Resolves a VST 3 controller number to its assigned parameter (via the cached
    /// <c>IMidiMapping</c> table) and queues a normalised change. In VST 3, CC / pitch-bend /
    /// aftertouch are parameter changes, not events.
    /// </summary>
    private bool RouteController(short controller, double normalizedValue, long arrivalTicks)
    {
        if (!_midiCcToParam.TryGetValue(controller, out var paramId))
        {
            return false;
        }
        var clamped = normalizedValue < 0 ? 0 : normalizedValue > 1 ? 1 : normalizedValue;
        var target = ComputeTargetSample(arrivalTicks == 0 ? Stopwatch.GetTimestamp() : arrivalTicks);
        _liveParamChanges.Enqueue((target, paramId, clamped));
        return true;
    }

    /// <summary>
    /// Queues a note-on to fire at sample offset 0 of the next <see cref="Process"/> call, in arrival
    /// order. This is the counterpart to <see cref="SendNoteOn"/> for <em>segment-driven</em> hosts —
    /// an offline renderer, or a <c>SequencedMidiPlayer</c> that has already split the audio block at
    /// each event's frame so every event belongs at the start of the next (sub-)block. Unlike
    /// <see cref="SendNoteOn"/> it consults no wall clock, so it is correct under faster-than-real-time
    /// rendering. Call it on the same thread as <see cref="Process"/> (the audio/render thread); for
    /// cross-thread live input use <see cref="SendNoteOn"/> instead.
    /// </summary>
    /// <param name="pitch">MIDI note number (0–127).</param>
    /// <param name="velocity">Normalised velocity in [0, 1].</param>
    /// <param name="channel">Event-bus channel (0-based).</param>
    /// <exception cref="InvalidOperationException">The plug-in is not an instrument.</exception>
    public void EnqueueNoteOn(int pitch, float velocity, int channel = 0)
    {
        EnsureInstrument();
        _activeNotes[(channel << 8) | (pitch & 0xFF)] = 1;
        _immediateEvents.Enqueue(BuildNoteEvent(EventType.NoteOn, channel, pitch, velocity));
    }

    /// <summary>Queues a note-off to fire at offset 0 of the next block. See <see cref="EnqueueNoteOn"/>.</summary>
    public void EnqueueNoteOff(int pitch, float velocity = 0f, int channel = 0)
    {
        EnsureInstrument();
        _activeNotes.TryRemove((channel << 8) | (pitch & 0xFF), out _);
        _immediateEvents.Enqueue(BuildNoteEvent(EventType.NoteOff, channel, pitch, velocity));
    }

    /// <summary>
    /// Routes a MIDI control change to its assigned parameter, to fire at offset 0 of the next block.
    /// The segment-driven counterpart to <see cref="SendControlChange"/>; returns <c>false</c> if the
    /// plug-in doesn't map that CC. See <see cref="EnqueueNoteOn"/>.
    /// </summary>
    public bool EnqueueControlChange(int controllerNumber, double normalizedValue)
        => RouteControllerImmediate((short)controllerNumber, normalizedValue);

    /// <summary>Routes pitch-bend at offset 0 of the next block. See <see cref="EnqueueControlChange"/>.</summary>
    public bool EnqueuePitchBend(double normalizedValue)
        => RouteControllerImmediate(Vst3ControllerNumbers.PitchBend, normalizedValue);

    /// <summary>Routes channel pressure / aftertouch at offset 0 of the next block. See <see cref="EnqueueControlChange"/>.</summary>
    public bool EnqueueChannelPressure(double normalizedValue)
        => RouteControllerImmediate(Vst3ControllerNumbers.AfterTouch, normalizedValue);

    /// <summary>
    /// Selects a program by MIDI program number, to take effect at offset 0 of the next block — the
    /// segment-driven counterpart to <see cref="SendProgramChange"/>. Returns <c>false</c> if the plug-in
    /// has no program-change parameter. See <see cref="EnqueueNoteOn"/>.
    /// </summary>
    public bool EnqueueProgramChange(int program)
    {
        if (!_hasProgramChange) return false;
        _immediateParamChanges.Enqueue((_programChangeParamId, NormalizedProgram(program)));
        return true;
    }

    /// <summary>
    /// Sends a System Exclusive message to the instrument as a VST 3 <c>DataEvent</c>, delivered live on
    /// the next block. Safe to call from any thread. <paramref name="data"/> is the raw MIDI SysEx
    /// message including the leading <c>F0</c> and trailing <c>F7</c>; it is copied, so the caller's
    /// buffer can be reused immediately. Empty messages are ignored. Most instruments ignore SysEx; this
    /// just delivers it. See <see cref="SendNoteOn"/> for the <paramref name="arrivalTicks"/> timing.
    /// </summary>
    public void SendSysEx(ReadOnlySpan<byte> data, long arrivalTicks = 0)
    {
        EnsureInstrument();
        if (data.Length == 0) return;
        var target = ComputeTargetSample(arrivalTicks == 0 ? Stopwatch.GetTimestamp() : arrivalTicks);
        _liveEvents.Enqueue((target, BuildDataEvent(data)));
    }

    /// <summary>
    /// Queues a System Exclusive message to fire at offset 0 of the next block — the segment-driven
    /// counterpart to <see cref="SendSysEx"/>. <paramref name="data"/> is the raw SysEx message
    /// (<c>F0</c>…<c>F7</c>), copied; empty messages are ignored. See <see cref="EnqueueNoteOn"/>.
    /// </summary>
    public void EnqueueSysEx(ReadOnlySpan<byte> data)
    {
        EnsureInstrument();
        if (data.Length == 0) return;
        _immediateEvents.Enqueue(BuildDataEvent(data));
    }

    // Builds a VST 3 DataEvent (type kMidiSysEx) over a freshly-allocated unmanaged copy of the bytes.
    // The buffer is owned by the host until the process() call that consumes the event returns, then
    // freed by FreeSysExBuffers (or DrainQueuedSysExBuffers on dispose if never dispatched).
    private static unsafe Event BuildDataEvent(ReadOnlySpan<byte> data)
    {
        var buffer = Marshal.AllocHGlobal(data.Length);
        data.CopyTo(new Span<byte>((void*)buffer, data.Length));
        var ev = new Event
        {
            BusIndex = 0,
            SampleOffset = 0,
            PpqPosition = 0,
            Flags = (ushort)EventFlags.IsLive,
            Type = (ushort)EventType.Data,
        };
        *(DataEvent*)ev.UnionData = new DataEvent { Size = (uint)data.Length, Type = 0 /* kMidiSysEx */, Bytes = buffer };
        return ev;
    }

    // Adds an event to the per-block host event list, recording any SysEx (Data) event's unmanaged
    // buffer so it can be freed once process() has consumed it.
    private unsafe void EmitInputEvent(Event ev)
    {
        _inputEvents!.Add(ev);
        if (ev.Type == (ushort)EventType.Data)
        {
            _sysexBuffersThisBlock.Add(((DataEvent*)ev.UnionData)->Bytes);
        }
    }

    // Frees the unmanaged SysEx buffers handed to the plug-in this block. Called right after process()
    // returns (whether or not it succeeded) — the plug-in reads the bytes synchronously during the call.
    private void FreeSysExBuffers()
    {
        for (var i = 0; i < _sysexBuffersThisBlock.Count; i++)
        {
            if (_sysexBuffersThisBlock[i] != IntPtr.Zero) Marshal.FreeHGlobal(_sysexBuffersThisBlock[i]);
        }
        _sysexBuffersThisBlock.Clear();
    }

    // Frees SysEx buffers for events still sitting un-dispatched in the queues at dispose time (so a
    // SysEx enqueued but never processed doesn't leak). Best-effort; runs on the disposing thread.
    private unsafe void DrainQueuedSysExBuffers()
    {
        while (_immediateEvents.TryDequeue(out var ev))
        {
            if (ev.Type == (ushort)EventType.Data) Marshal.FreeHGlobal(((DataEvent*)ev.UnionData)->Bytes);
        }
        while (_liveEvents.TryDequeue(out var entry))
        {
            var ev = entry.Ev;
            if (ev.Type == (ushort)EventType.Data) Marshal.FreeHGlobal(((DataEvent*)ev.UnionData)->Bytes);
        }
        foreach (var entry in _pendingLiveEvents)
        {
            var ev = entry.Ev;
            if (ev.Type == (ushort)EventType.Data) Marshal.FreeHGlobal(((DataEvent*)ev.UnionData)->Bytes);
        }
        _pendingLiveEvents.Clear();
    }

    /// <summary>Segment-driven counterpart to <see cref="RouteController"/> — queues at offset 0, no wall clock.</summary>
    private bool RouteControllerImmediate(short controller, double normalizedValue)
    {
        if (!_midiCcToParam.TryGetValue(controller, out var paramId))
        {
            return false;
        }
        var clamped = normalizedValue < 0 ? 0 : normalizedValue > 1 ? 1 : normalizedValue;
        _immediateParamChanges.Enqueue((paramId, clamped));
        return true;
    }

    // MIDI program N → normalised value for the program-change parameter. VST 3 discrete parameters map
    // plain value P (the program index, 0..StepCount) to P / StepCount; out-of-range programs clamp to
    // the last. Fall back to /127 if the parameter reports no steps (unusual for a program list).
    private double NormalizedProgram(int program)
    {
        var span = _programChangeStepCount > 0 ? _programChangeStepCount : 127;
        var value = program / (double)span;
        return value < 0 ? 0 : value > 1 ? 1 : value;
    }

    // Resolve the program-change target once: the first parameter flagged IsProgramChange (the root
    // unit's program list in the common case). Absent on most synths, present on multi-program ones.
    private void CacheProgramChangeParameter()
    {
        foreach (var parameter in Parameters)
        {
            if (!parameter.IsProgramChange) continue;
            _hasProgramChange = true;
            _programChangeParamId = parameter.Id;
            _programChangeStepCount = parameter.StepCount;
            _programChangeUnitId = parameter.UnitId;
            return;
        }
    }

    /// <summary>
    /// Sets the musical/transport context (tempo, time signature, playhead, playing state) the host
    /// presents to the instrument's <c>ProcessContext</c> on subsequent <see cref="Process"/> calls,
    /// so tempo-following plug-ins lock to the host timeline. Push a fresh snapshot per block (as
    /// <see cref="Vst3MidiInstrument"/> does from a sequencer <c>Transport</c>). Call it on the audio
    /// thread. Until set — or after <see cref="ClearMusicalContext"/> — the instrument gets a
    /// free-running 120-BPM, 4/4, stopped context. No effect on effects (they receive no context).
    /// </summary>
    public void SetMusicalContext(in Vst3MusicalContext context) => _musicalContext = context;

    /// <summary>Reverts to the free-running default context. See <see cref="SetMusicalContext"/>.</summary>
    public void ClearMusicalContext() => _musicalContext = null;

    // Called from Vst3ComponentHandler when the plug-in raises restartComponent. We act on the latency
    // flag (the one that affects this host's processing); the rest — parameter values, bus routing — are
    // re-read lazily on the next query, so no action is needed here.
    private void OnRestartComponent(int flags)
    {
        if ((flags & (int)RestartFlags.LatencyChanged) == 0 || _processor is null) return;
        var updated = (int)_processor.GetLatencySamples();
        if (Volatile.Read(ref _latencySamples) == updated) return;
        Volatile.Write(ref _latencySamples, updated);
        LatencyChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Translates a producer-side arrival timestamp (in <see cref="Stopwatch.GetTimestamp"/>
    /// ticks) into a target absolute sample on this plug-in's audio clock, by interpolating
    /// from the block-start snapshot captured at the top of the most recent
    /// <see cref="Process"/> call. Before the first Process call (clock not yet snapped),
    /// targets at sample 0 so events fire on the first block.
    /// </summary>
    private long ComputeTargetSample(long arrivalTicks)
    {
        long blockStartSample, blockStartTicks;
        lock (_clockLock)
        {
            blockStartSample = _blockStartSample;
            blockStartTicks = _blockStartTimestampTicks;
        }
        if (blockStartTicks == 0)
        {
            return 0;
        }
        var elapsedTicks = arrivalTicks - blockStartTicks;
        var elapsedSamples = (long)((double)elapsedTicks * SampleRate / Stopwatch.Frequency);
        return blockStartSample + elapsedSamples;
    }

    /// <summary>Moves every queued live event off the producer-side concurrent queue and onto
    /// the audio-thread-owned <see cref="_pendingLiveEvents"/> list. Idempotent / cheap when
    /// the queue is empty.</summary>
    private void DrainLiveEventsIntoPending()
    {
        while (_liveEvents.TryDequeue(out var entry))
        {
            _pendingLiveEvents.Add(entry);
        }
        // Sort once per drain — handles out-of-order arrivals across multiple producer threads.
        // Bulk of the time the list is already in order (single MIDI producer) so the sort cost
        // is the comparison-only fast path.
        if (_pendingLiveEvents.Count > 1)
        {
            _pendingLiveEvents.Sort(static (a, b) => a.TargetSample.CompareTo(b.TargetSample));
        }
    }

    /// <summary>Adds every pending live event whose target falls in the current block to the
    /// native event list with the right sample offset, and removes those entries. Entries
    /// scheduled past <paramref name="blockEnd"/> stay on the list for the next block.</summary>
    private void DispatchPendingLiveEvents(long blockEnd, int numSamples)
    {
        var fired = 0;
        for (var i = 0; i < _pendingLiveEvents.Count; i++)
        {
            var entry = _pendingLiveEvents[i];
            if (entry.TargetSample >= blockEnd) break;
            var offset = entry.TargetSample - _samplePosition;
            var ev = entry.Ev;
            ev.SampleOffset = (int)Math.Clamp(offset, 0, numSamples - 1);
            EmitInputEvent(ev);
            fired++;
        }
        if (fired > 0) _pendingLiveEvents.RemoveRange(0, fired);
    }

    /// <summary>See <see cref="DrainLiveEventsIntoPending"/> — same shape for live CC / pitch-bend / aftertouch.</summary>
    private void DrainLiveParamChangesIntoPending()
    {
        while (_liveParamChanges.TryDequeue(out var entry))
        {
            _pendingLiveParamChanges.Add(entry);
        }
        if (_pendingLiveParamChanges.Count > 1)
        {
            _pendingLiveParamChanges.Sort(static (a, b) => a.TargetSample.CompareTo(b.TargetSample));
        }
    }

    /// <summary>True if at least one pending live param change targets the current block.</summary>
    private bool HasPendingParamChangesInBlock(long blockEnd)
    {
        return _pendingLiveParamChanges.Count > 0
            && _pendingLiveParamChanges[0].TargetSample < blockEnd;
    }

    /// <summary>Adds every pending live param change whose target falls in the current block to
    /// the host-side IParameterChanges with the right sample offset, and removes those entries.</summary>
    private void DispatchPendingParamChanges(long blockEnd, int numSamples)
    {
        var fired = 0;
        for (var i = 0; i < _pendingLiveParamChanges.Count; i++)
        {
            var entry = _pendingLiveParamChanges[i];
            if (entry.TargetSample >= blockEnd) break;
            var offset = entry.TargetSample - _samplePosition;
            var clamped = (int)Math.Clamp(offset, 0, numSamples - 1);
            _inputChanges!.AcquireQueue(entry.Id).Append(clamped, entry.Value);
            fired++;
        }
        if (fired > 0) _pendingLiveParamChanges.RemoveRange(0, fired);
    }

    private void BuildMidiControllerMap()
    {
        if (_midiMappingPtr == IntPtr.Zero)
        {
            return;
        }
        IMidiMapping? mapping = null;
        try
        {
            mapping = (IMidiMapping)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
                _midiMappingPtr, CreateObjectFlags.UniqueInstance);
            // Resolve the common controllers on bus 0 / channel 0. Per-channel resolution is a later
            // refinement — most instruments map identically across channels.
            for (short controller = 0; controller <= Vst3ControllerNumbers.PitchBend; controller++)
            {
                if (mapping.GetMidiControllerAssignment(0, 0, controller, out var paramId) == TResultCodes.Ok)
                {
                    _midiCcToParam[controller] = paramId;
                }
            }
        }
        catch
        {
            // IMidiMapping is optional; treat any failure as "no controller routing".
        }
        finally
        {
            if (mapping is not null)
            {
                ((ComObject)(object)mapping).FinalRelease();
            }
            Marshal.Release(_midiMappingPtr);
            _midiMappingPtr = IntPtr.Zero;
        }
    }

    // Consume the QI'd IUnitInfo pointer into the public Units / ProgramLists collections. Best-effort:
    // IUnitInfo is optional, and a plug-in that implements it partially still yields what it can.
    private void BuildUnitModel()
    {
        if (_unitInfoPtr == IntPtr.Zero)
        {
            return; // plug-in exposes no IUnitInfo — Units / ProgramLists stay empty
        }
        IUnitInfo? unitInfo = null;
        try
        {
            unitInfo = (IUnitInfo)Vst3ComWrappers.Instance.GetOrCreateObjectForComInstance(
                _unitInfoPtr, CreateObjectFlags.UniqueInstance);

            var listCount = unitInfo.GetProgramListCount();
            if (listCount > 0)
            {
                var lists = new List<Vst3ProgramList>(listCount);
                for (var i = 0; i < listCount; i++)
                {
                    if (unitInfo.GetProgramListInfo(i, out var info) != TResultCodes.Ok) continue;
                    var name = ReadFixedUtf16(info.Name, 128);
                    var programs = ReadProgramNames(unitInfo, info.Id, info.ProgramCount);
                    lists.Add(new Vst3ProgramList(info.Id, name, programs));
                }
                if (lists.Count > 0) ProgramLists = lists;
            }

            var unitCount = unitInfo.GetUnitCount();
            if (unitCount > 0)
            {
                var units = new List<Vst3Unit>(unitCount);
                for (var i = 0; i < unitCount; i++)
                {
                    if (unitInfo.GetUnitInfo(i, out var info) != TResultCodes.Ok) continue;
                    var name = ReadFixedUtf16(info.Name, 128);
                    units.Add(new Vst3Unit(info.Id, info.ParentUnitId, name, info.ProgramListId));
                }
                if (units.Count > 0) Units = units;
            }

            ActiveProgramList = ResolveActiveProgramList();
        }
        catch
        {
            // IUnitInfo is optional; treat any failure as "no unit / program info".
        }
        finally
        {
            if (unitInfo is not null)
            {
                ((ComObject)(object)unitInfo).FinalRelease();
            }
            Marshal.Release(_unitInfoPtr);
            _unitInfoPtr = IntPtr.Zero;
        }
    }

    private static List<string> ReadProgramNames(IUnitInfo unitInfo, int listId, int programCount)
    {
        var names = new List<string>(Math.Max(0, programCount));
        const int capacity = 128;
        Span<char> buffer = stackalloc char[capacity];
        for (var p = 0; p < programCount; p++)
        {
            buffer.Clear();
            var name = string.Empty;
            fixed (char* bufPtr = buffer)
            {
                if (unitInfo.GetProgramName(listId, p, (IntPtr)bufPtr) == TResultCodes.Ok)
                {
                    var slice = buffer;
                    var nul = slice.IndexOf('\0');
                    if (nul >= 0) slice = slice[..nul];
                    name = new string(slice);
                }
            }
            names.Add(name);
        }
        return names;
    }

    // The program list the program-change parameter drives: resolved via that parameter's owning unit
    // (unit → programListId → list), falling back to the sole list when there's exactly one.
    private Vst3ProgramList? ResolveActiveProgramList()
    {
        if (ProgramLists.Count == 0) return null;
        if (_hasProgramChange)
        {
            foreach (var unit in Units)
            {
                if (unit.Id == _programChangeUnitId &&
                    unit.ProgramListId != Vst3UnitConstants.NoProgramListId)
                {
                    var match = FindProgramList(unit.ProgramListId);
                    if (match is not null) return match;
                }
            }
        }
        return ProgramLists.Count == 1 ? ProgramLists[0] : null;
    }

    private Vst3ProgramList? FindProgramList(int id)
    {
        foreach (var list in ProgramLists)
        {
            if (list.Id == id) return list;
        }
        return null;
    }

    private void ScheduleNote(long sampleTime, EventType type, int channel, int pitch, float velocity)
    {
        EnsureInstrument();
        ArgumentOutOfRangeException.ThrowIfNegative(sampleTime);
        _scheduledEvents.Add(new ScheduledEvent(sampleTime, BuildNoteEvent(type, channel, pitch, velocity)));
        _scheduleSorted = false;
    }

    private void EnsureInstrument()
    {
        if (!_isInstrument)
        {
            throw new InvalidOperationException(
                $"'{ClassInfo.Name}' is not an instrument; note input is unavailable.");
        }
    }

    private static Event BuildNoteEvent(EventType type, int channel, int pitch, float velocity)
    {
        var ev = new Event
        {
            BusIndex = 0,
            SampleOffset = 0,
            PpqPosition = 0,
            Flags = (ushort)EventFlags.IsLive,
            Type = (ushort)type,
        };
        if (type == EventType.NoteOn)
        {
            var note = new NoteOnEvent
            {
                Channel = (short)channel,
                Pitch = (short)pitch,
                Tuning = 0f,
                Velocity = velocity,
                Length = 0,
                NoteId = -1,
            };
            *(NoteOnEvent*)ev.UnionData = note;
        }
        else
        {
            var note = new NoteOffEvent
            {
                Channel = (short)channel,
                Pitch = (short)pitch,
                Velocity = velocity,
                NoteId = -1,
                Tuning = 0f,
            };
            *(NoteOffEvent*)ev.UnionData = note;
        }
        return ev;
    }

    private ProcessContext BuildProcessContext(long sampleTime)
    {
        // Bit values from Vst::ProcessContext::StatesAndFlags.
        const uint kPlaying = 1 << 1;
        const uint kProjectTimeMusicValid = 1 << 9;
        const uint kTempoValid = 1 << 10;
        const uint kBarPositionValid = 1 << 11;
        const uint kTimeSigValid = 1 << 13;
        const uint kContTimeValid = 1 << 17;

        // Sequencer-driven context: emit the tempo / time-signature / position / playing state the host
        // pushed via SetMusicalContext (e.g. Vst3MidiInstrument from a Transport), so tempo-following
        // plug-ins lock to the timeline.
        if (_musicalContext is Vst3MusicalContext mc)
        {
            var state = kProjectTimeMusicValid | kTempoValid | kBarPositionValid | kTimeSigValid | kContTimeValid;
            if (mc.IsPlaying) state |= kPlaying;
            return new ProcessContext
            {
                State = state,
                SampleRate = SampleRate,
                ProjectTimeSamples = mc.ProjectTimeSamples,
                ContinousTimeSamples = mc.ProjectTimeSamples,
                ProjectTimeMusic = mc.ProjectTimeMusic,
                BarPositionMusic = mc.BarPositionMusic,
                Tempo = mc.Tempo,
                TimeSigNumerator = mc.TimeSigNumerator,
                TimeSigDenominator = mc.TimeSigDenominator,
            };
        }

        // Free-running fallback (live keyboard, scripted ScheduleNoteOn): tempo + time-signature +
        // continuous/musical time marked valid, transport stopped.
        const double tempo = 120.0;
        var musicTime = SampleRate > 0 ? (sampleTime / (double)SampleRate) * (tempo / 60.0) : 0.0;
        return new ProcessContext
        {
            State = kProjectTimeMusicValid | kTempoValid | kTimeSigValid | kContTimeValid,
            SampleRate = SampleRate,
            ProjectTimeSamples = sampleTime,
            ContinousTimeSamples = sampleTime,
            ProjectTimeMusic = musicTime,
            Tempo = tempo,
            TimeSigNumerator = 4,
            TimeSigDenominator = 4,
        };
    }

    /// <summary>An event scheduled against the plug-in's absolute sample timeline.</summary>
    private readonly struct ScheduledEvent(long sampleTime, Event nativeEvent)
    {
        public long SampleTime { get; } = sampleTime;
        public Event NativeEvent { get; } = nativeEvent;
    }

    /// <inheritdoc/>
    public void Dispose() => DisposeCore();

    private void DisposeCore()
    {
        // Free unmanaged SysEx buffers for any events that were queued but never processed.
        FreeSysExBuffers();
        DrainQueuedSysExBuffers();

        if (_processing && _processor is not null)
        {
            _processor.SetProcessing(0);
            _processing = false;
        }
        if (_active && _component is not null)
        {
            _component.SetActive(0);
            _active = false;
        }

        // Disconnect the IConnectionPoint pair before terminating either half. Vtable slot 4
        // is Disconnect; per SDK convention we pass IntPtr.Zero to disconnect from "any".
        if (_connected)
        {
            unsafe
            {
                if (_componentCpPtr != IntPtr.Zero)
                {
                    var vt = *(IntPtr**)_componentCpPtr;
                    var disc = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)vt[4];
                    try { disc(_componentCpPtr, IntPtr.Zero); } catch { /* best effort */ }
                }
                if (_controllerCpPtr != IntPtr.Zero)
                {
                    var vt = *(IntPtr**)_controllerCpPtr;
                    var disc = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)vt[4];
                    try { disc(_controllerCpPtr, IntPtr.Zero); } catch { /* best effort */ }
                }
            }
            _connected = false;
        }
        if (_componentCpPtr != IntPtr.Zero)
        {
            Marshal.Release(_componentCpPtr);
            _componentCpPtr = IntPtr.Zero;
        }
        if (_controllerCpPtr != IntPtr.Zero)
        {
            Marshal.Release(_controllerCpPtr);
            _controllerCpPtr = IntPtr.Zero;
        }

        if (_controllerInitialized && _controller is not null)
        {
            try { _controller.Terminate(); } catch { /* best effort */ }
            _controllerInitialized = false;
        }
        if (_initialized && _component is not null)
        {
            _component.Terminate();
            _initialized = false;
        }

        FreeIfNonNull(ref _inputBus);
        FreeIfNonNull(ref _outputBus);
        FreeIfNonNull(ref _inputChannelPtrs);
        FreeIfNonNull(ref _outputChannelPtrs);
        FreePerChannelBuffers(ref _inputBuffers, _inputChannels);
        FreePerChannelBuffers(ref _outputBuffers, _outputChannels);

        if (_inputChangesPtr != IntPtr.Zero)
        {
            Marshal.Release(_inputChangesPtr);
            _inputChangesPtr = IntPtr.Zero;
        }
        _inputChanges?.Dispose();
        _inputChanges = null;

        if (_inputEventsPtr != IntPtr.Zero)
        {
            Marshal.Release(_inputEventsPtr);
            _inputEventsPtr = IntPtr.Zero;
        }
        _inputEvents = null;

        if (_midiMappingPtr != IntPtr.Zero)
        {
            Marshal.Release(_midiMappingPtr);
            _midiMappingPtr = IntPtr.Zero;
        }

        // Normally consumed (released + zeroed) by BuildUnitModel; this covers construction failing
        // before that ran.
        if (_unitInfoPtr != IntPtr.Zero)
        {
            Marshal.Release(_unitInfoPtr);
            _unitInfoPtr = IntPtr.Zero;
        }

        if (_processor is not null)
        {
            ((ComObject)(object)_processor).FinalRelease();
            _processor = null;
        }
        if (_controller is not null)
        {
            ((ComObject)(object)_controller).FinalRelease();
            _controller = null;
        }
        if (_component is not null)
        {
            ((ComObject)(object)_component).FinalRelease();
            _component = null;
        }
        if (_componentHandlerUnknown != IntPtr.Zero)
        {
            Marshal.Release(_componentHandlerUnknown);
            _componentHandlerUnknown = IntPtr.Zero;
        }
        if (_hostUnknown != IntPtr.Zero)
        {
            Marshal.Release(_hostUnknown);
            _hostUnknown = IntPtr.Zero;
        }
        _hostApp = null;
    }

    private static void FreeIfNonNull<T>(ref T* p) where T : unmanaged
    {
        if (p is not null)
        {
            NativeMemory.Free(p);
            p = null;
        }
    }

    private static void FreePerChannelBuffers(ref float** buffers, int channelCount)
    {
        if (buffers is null) return;
        for (var c = 0; c < channelCount; c++)
        {
            if (buffers[c] is not null)
            {
                NativeMemory.Free(buffers[c]);
                buffers[c] = null;
            }
        }
        NativeMemory.Free(buffers);
        buffers = null;
    }
}
