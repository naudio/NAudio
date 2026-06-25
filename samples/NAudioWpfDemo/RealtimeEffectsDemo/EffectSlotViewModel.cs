using System;
using System.Collections.ObjectModel;
using NAudio.Effects;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.RealtimeEffectsDemo;

/// <summary>
/// One effect in the chain: its generic parameters plus the Bypass/Mix that every
/// <see cref="AudioEffect"/> provides, with reorder/remove commands.
/// </summary>
class EffectSlotViewModel : ViewModelBase
{
    private readonly AudioEffect effect;

    public EffectSlotViewModel(string name, IAudioEffect effect,
        Action<EffectSlotViewModel> remove,
        Action<EffectSlotViewModel> moveUp,
        Action<EffectSlotViewModel> moveDown)
    {
        Name = name;
        Effect = effect;
        this.effect = effect as AudioEffect;

        Parameters = new ObservableCollection<ParameterViewModel>();
        if (effect is IParameterized parameterized)
            foreach (var p in parameterized.Parameters)
                Parameters.Add(new ParameterViewModel(p));

        RemoveCommand = new DelegateCommand(() => remove(this));
        MoveUpCommand = new DelegateCommand(() => moveUp(this));
        MoveDownCommand = new DelegateCommand(() => moveDown(this));
    }

    public string Name { get; }

    public IAudioEffect Effect { get; }

    public ObservableCollection<ParameterViewModel> Parameters { get; }

    public bool HasMix => effect != null;

    public bool Bypass
    {
        get => effect != null && effect.Bypass;
        set { if (effect != null) { effect.Bypass = value; OnPropertyChanged(nameof(Bypass)); } }
    }

    public double Mix
    {
        get => effect?.Mix ?? 1.0;
        set { if (effect != null) { effect.Mix = (float)value; OnPropertyChanged(nameof(Mix)); } }
    }

    public DelegateCommand RemoveCommand { get; }

    public DelegateCommand MoveUpCommand { get; }

    public DelegateCommand MoveDownCommand { get; }

    /// <summary>Refresh meter/automation-driven parameter values for the UI.</summary>
    public void RefreshMeters()
    {
        foreach (var p in Parameters)
            if (p.ShowMeter)
                p.Refresh();
    }
}
