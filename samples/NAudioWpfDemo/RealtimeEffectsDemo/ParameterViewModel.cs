using System.Collections.Generic;
using NAudio.Effects;
using NAudioWpfDemo.ViewModel;

namespace NAudioWpfDemo.RealtimeEffectsDemo;

/// <summary>
/// Binds a single <see cref="EffectParameter"/> to the generic editor. One control
/// per kind is shown in the DataTemplate; the <c>Show*</c> flags pick which.
/// </summary>
internal class ParameterViewModel : ViewModelBase
{
    private readonly EffectParameter parameter;

    public ParameterViewModel(EffectParameter parameter)
    {
        this.parameter = parameter;
    }

    public string Name => parameter.Name;

    public string Unit => parameter.Unit;

    public double Minimum => parameter.Minimum;

    public double Maximum => parameter.Maximum;

    public bool ShowSlider => parameter.Kind == EffectParameterKind.Continuous;

    public bool ShowToggle => parameter.Kind == EffectParameterKind.Toggle;

    public bool ShowChoice => parameter.Kind == EffectParameterKind.Choice;

    public bool ShowMeter => parameter.Kind == EffectParameterKind.Meter;

    public IReadOnlyList<string> Choices => parameter.Choices;

    public double Value
    {
        get => parameter.Value;
        set
        {
            parameter.Value = (float)value;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DisplayValue));
        }
    }

    public bool BoolValue
    {
        get => parameter.Value >= 0.5f;
        set
        {
            parameter.Value = value ? 1f : 0f;
            OnPropertyChanged(nameof(BoolValue));
        }
    }

    public int ChoiceIndex
    {
        get => (int)parameter.Value;
        set
        {
            parameter.Value = value;
            OnPropertyChanged(nameof(ChoiceIndex));
        }
    }

    public string DisplayValue => $"{parameter.Value:0.##} {Unit}".Trim();

    /// <summary>Pushes external changes (meters, automation) to the UI.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(BoolValue));
        OnPropertyChanged(nameof(ChoiceIndex));
        OnPropertyChanged(nameof(DisplayValue));
    }
}
