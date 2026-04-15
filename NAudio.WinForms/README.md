# NAudio.WinForms

[![Nuget](https://img.shields.io/nuget/v/NAudio.WinForms)](https://www.nuget.org/packages/NAudio.WinForms/)

Windows Forms controls for [NAudio](https://github.com/naudio/NAudio). Windows-only (`net8.0-windows`, `UseWindowsForms=true`).

## What's included

A handful of reusable GUI controls useful when building audio apps with WinForms:

- `Fader` — vertical channel fader
- `VolumeSlider` — horizontal volume slider
- `PanSlider` — pan control
- `Pot` — rotary knob
- `WaveViewer` — render a `WaveStream` as a waveform
- `ProgressLog` — progress/log text control

## When to use it

Install this package only if you are building a Windows Forms application and want ready-made audio controls. For WPF apps, use the `NAudioWpfDemo` project in the NAudio source tree as a reference and roll your own controls — there is no `NAudio.Wpf` package.

See the [NAudio GitHub repository](https://github.com/naudio/NAudio) for full documentation and demos.

## License

MIT.
