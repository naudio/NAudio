using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NAudioWpfDemo.Utils;

namespace NAudioWpfDemo.AudioPlaybackDemo;

/// <summary>
/// Visualization plugin that wraps <see cref="LiveWaveformControl"/> with a small options panel
/// so the user can flip through render styles and scales live.
/// </summary>
internal class LiveWaveformVisualization : IVisualizationPlugin
{
    private readonly LiveWaveformControl waveform = new();
    private readonly DockPanel content;

    public LiveWaveformVisualization()
    {
        content = new DockPanel();

        var optionsPanel = BuildOptionsPanel(waveform);
        DockPanel.SetDock(optionsPanel, Dock.Bottom);
        content.Children.Add(optionsPanel);
        content.Children.Add(waveform);
    }

    public string Name => "Live Waveform";

    public object Content => content;

    public void OnMaxCalculated(float min, float max)
    {
        waveform.AddValue(max, min);
    }

    public void OnFftCalculated(NAudio.Dsp.Complex[] result)
    {
        // nothing to do — waveform visualization doesn't care about FFT
    }

    public void OnSourceChanged(int sampleRate)
    {
        // Clear history on file change so we don't mix the tail of the last track into the
        // new one's opening milliseconds.
        waveform.Reset();
    }

    private static UIElement BuildOptionsPanel(LiveWaveformControl target)
    {
        // A WrapPanel flows onto a second line when the window is narrow. Each logical knob
        // is a small inner StackPanel (label + control) so the label never separates from its
        // control across a wrap boundary.
        var panel = new WrapPanel { Margin = new Thickness(4) };

        // Style, scale, top-half and samples/column apply to every render style, so they're
        // always shown. The remaining two only affect one style each, and are collapsed when
        // that style isn't selected rather than sitting there doing nothing.
        panel.Children.Add(BuildComboGroup("Style:", 110, typeof(WaveformRenderStyle),
            target, nameof(LiveWaveformControl.RenderStyle)));
        panel.Children.Add(BuildComboGroup("Scale:", 90, typeof(WaveformVerticalScale),
            target, nameof(LiveWaveformControl.VerticalScale)));
        panel.Children.Add(BuildCheckboxGroup("Top half only", null,
            target, nameof(LiveWaveformControl.TopHalfOnly)));
        panel.Children.Add(BuildSliderGroup("Samples/col:", 1, 8,
            target, nameof(LiveWaveformControl.SamplesPerColumn)));

        panel.Children.Add(ShowOnlyForStyle(
            BuildCheckboxGroup("Fill between lines", "Fills the region between the min and max curves",
                target, nameof(LiveWaveformControl.FillBetweenLines)),
            target, WaveformRenderStyle.MinMaxLines));
        panel.Children.Add(ShowOnlyForStyle(
            BuildSliderGroup("Bar width:", 1, 8,
                target, nameof(LiveWaveformControl.BarWidth)),
            target, WaveformRenderStyle.Bars));

        return panel;
    }

    /// <summary>
    /// Collapses <paramref name="element"/> whenever the control's render style isn't
    /// <paramref name="style"/>, so only the options that actually do something are on screen.
    /// </summary>
    private static FrameworkElement ShowOnlyForStyle(FrameworkElement element, LiveWaveformControl target, WaveformRenderStyle style)
    {
        element.SetBinding(UIElement.VisibilityProperty,
            new Binding(nameof(LiveWaveformControl.RenderStyle))
            {
                Source = target,
                Converter = new EnumToVisibilityConverter(),
                ConverterParameter = style
            });
        return element;
    }

    private static FrameworkElement BuildComboGroup(string label, double comboWidth, Type enumType, object source, string propertyName)
    {
        var group = NewInlineGroup();
        group.Children.Add(new Label { Content = label, VerticalAlignment = VerticalAlignment.Center });
        var combo = new ComboBox { Width = comboWidth, VerticalAlignment = VerticalAlignment.Center };
        foreach (var v in Enum.GetValues(enumType)) combo.Items.Add(v);
        combo.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty,
            new Binding(propertyName) { Source = source, Mode = BindingMode.TwoWay });
        group.Children.Add(combo);
        return group;
    }

    private static FrameworkElement BuildCheckboxGroup(string label, string toolTip, object source, string propertyName)
    {
        var group = NewInlineGroup();
        var check = new CheckBox
        {
            Content = label,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = toolTip
        };
        check.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(propertyName) { Source = source, Mode = BindingMode.TwoWay });
        group.Children.Add(check);
        return group;
    }

    private static FrameworkElement BuildSliderGroup(string label, double min, double max, object source, string propertyName)
    {
        var group = NewInlineGroup();
        group.Children.Add(new Label { Content = label, VerticalAlignment = VerticalAlignment.Center });
        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Width = 90,
            VerticalAlignment = VerticalAlignment.Center
        };
        slider.SetBinding(Slider.ValueProperty, new Binding(propertyName) { Source = source, Mode = BindingMode.TwoWay });
        group.Children.Add(slider);
        return group;
    }

    private static StackPanel NewInlineGroup() => new()
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 2, 10, 2)
    };
}
