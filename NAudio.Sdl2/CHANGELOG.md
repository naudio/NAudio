### 2.2.6 (15 Nov 2024)
* BREAKING CHANGES
* Remove `AudioConversion`
* Remove `ActualWaveFormat`
* Remove `AdjustLatencyPercent`
* Change `WaveInSdlCapabilities`, `WaveOutSdlCapabilities` classes to structs
* Added automatic initialization and cleanup of SDL2 resources. `WaveInSdl`, `WaveOutSdl` constructors can throw exceptions
* Added experimental android support which requires the `NAudio.Sdl2.Library.Android` package. Use the `NAudioAvaloniaDemo.Android` project as an example
* Changes in NAudioAvaloniaDemo

### 2.2.5 (24 May 2024)
* Fix `WaveInSdl` bugs

### 2.2.4 (23 May 2024)
* Port NAudioWpfDemo to NAudioAvaloniaDemo
* Fix `WaveOutSdl` bugs
* Fix `AudioConversion` enum

### 2.2.3 (20 May 2024)
* Fix SDL version comparison

### 2.2.2 (13 May 2024)
* Added volume mixer for `WaveOutSdl`
* More reliable audio format recognition
* Added compatibility with versions lower than SDL-2.0.16

### 2.2.1 (30 Apr 2024)
* Initial version, `WaveInSdl` and `WaveOutSdl` implementations