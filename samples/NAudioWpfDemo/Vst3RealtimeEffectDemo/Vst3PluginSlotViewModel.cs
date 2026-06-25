using System;
using NAudio.Vst3;
using NAudioWpfDemo.ViewModel;
using NAudioWpfDemo.Vst3Shared;

namespace NAudioWpfDemo.Vst3RealtimeEffectDemo;

/// <summary>
/// One slot in the realtime effect chain. Owns a class-info "recipe" and an optional
/// state blob; the live <see cref="Vst3Plugin"/> instance is created by the viewmodel
/// at the engine's current sample rate when the engine is running, and disposed (with
/// state preserved) on stop. The slot surfaces Bypass, Show-Editor and Remove commands
/// to the UI.
/// </summary>
class Vst3PluginSlotViewModel : ViewModelBase, IDisposable
{
    private bool bypass;
    private Vst3ChainSlot liveSlot;
    private Vst3PluginView liveView;
    private Vst3EditorWindow editorWindow;
    private byte[] savedState;

    public Vst3PluginSlotViewModel(
        Vst3ModuleInfo moduleInfo,
        Vst3ClassInfo classInfo,
        Action<Vst3PluginSlotViewModel> remove,
        Action<Vst3PluginSlotViewModel> moveUp,
        Action<Vst3PluginSlotViewModel> moveDown)
    {
        ModuleInfo = moduleInfo;
        ClassInfo = classInfo;
        RemoveCommand = new DelegateCommand(() => remove(this));
        MoveUpCommand = new DelegateCommand(() => moveUp(this));
        MoveDownCommand = new DelegateCommand(() => moveDown(this));
        ShowEditorCommand = new DelegateCommand(ShowEditor) { IsEnabled = false };
    }

    public Vst3ModuleInfo ModuleInfo { get; }
    public Vst3ClassInfo ClassInfo { get; }
    public string Name => ClassInfo.Name;
    public string ModuleName => ModuleInfo.Name;

    public DelegateCommand RemoveCommand { get; }
    public DelegateCommand MoveUpCommand { get; }
    public DelegateCommand MoveDownCommand { get; }
    public DelegateCommand ShowEditorCommand { get; }

    public bool Bypass
    {
        get => bypass;
        set
        {
            bypass = value;
            if (liveSlot != null) liveSlot.Bypass = value;
            OnPropertyChanged(nameof(Bypass));
        }
    }

    /// <summary>True while a live plug-in instance is held (engine running).</summary>
    public bool IsLive => liveSlot != null;

    /// <summary>The live chain slot, or null when the engine is not running.</summary>
    public Vst3ChainSlot LiveSlot => liveSlot;

    /// <summary>Saved component+controller state, persisted across engine restarts.</summary>
    public byte[] SavedState => savedState;

    /// <summary>
    /// Creates a live plug-in instance at the given sample rate and max block size,
    /// restoring previously-saved state if available. Called by the viewmodel when the
    /// engine starts, or when this slot is added to a running engine.
    /// </summary>
    public void GoLive(Vst3Module module, int sampleRate, int maxBlockSize)
    {
        if (liveSlot != null) return;
        var plugin = module.CreatePlugin(ClassInfo, sampleRate, maxBlockSize);
        if (savedState != null)
        {
            try { plugin.LoadState(savedState); }
            catch { /* tolerate state mismatch — plug-in stays at its defaults */ }
        }
        liveSlot = new Vst3ChainSlot(plugin) { Bypass = bypass };
        liveView = plugin.CreateView();
        ShowEditorCommand.IsEnabled = liveView != null;
        OnPropertyChanged(nameof(IsLive));
    }

    /// <summary>
    /// Saves the current state, closes the editor window if open, and disposes the live
    /// plug-in. Called by the viewmodel when the engine stops, or when this slot is
    /// removed from a running engine. The slot can be re-lived later via
    /// <see cref="GoLive"/>.
    /// </summary>
    public void GoOffline()
    {
        if (liveSlot == null) return;
        try { savedState = liveSlot.Plugin.SaveState(); }
        catch { /* keep previous state — better than losing it */ }

        // Close the editor window first so it detaches before the plug-in is disposed.
        CloseEditorWindow();
        liveView?.Dispose();
        liveView = null;
        liveSlot.Plugin.Dispose();
        liveSlot = null;
        ShowEditorCommand.IsEnabled = false;
        OnPropertyChanged(nameof(IsLive));
    }

    private void ShowEditor()
    {
        if (liveView == null) return;
        if (editorWindow != null)
        {
            editorWindow.Activate();
            return;
        }
        editorWindow = new Vst3EditorWindow($"{ModuleName} — {Name}", liveView);
        editorWindow.ClosedByUser += (s, e) => editorWindow = null;
        editorWindow.Show();
    }

    private void CloseEditorWindow()
    {
        if (editorWindow == null) return;
        var w = editorWindow;
        editorWindow = null;
        try { w.Close(); }
        catch { /* already closing */ }
    }

    public void Dispose()
    {
        GoOffline();
    }
}
