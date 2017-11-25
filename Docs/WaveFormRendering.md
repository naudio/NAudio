# Render an Audio Wave Form to PNG

 NAudio does not include any visualization code in the core library, but it does provid  access to the raw audio samples which you need to render wave-forms.

 NAudio does however provide a sample project at GitHub: [NAudio.WaveFormRenderer](https://github.com/naudio/NAudio.WaveFormRenderer) which makes use of `NAudio` and `System.Drawing` to render waveforms in a variety of styles.

![Orange Blocks](https://cloud.githubusercontent.com/assets/147668/18606778/5a9516ac-7cb1-11e6-8660-a0a80d72fe26.png)

 ## WaveFormRendererLib

 The `WaveFormRendererLib` project contains a customizable waveform rendering algorithm, allowing you to 

 The waveform rendering algorithm is customizable:

 - Supports several peak calculation strategies (max, average, sampled, RMS, decibels)
 - Supports different colors or gradients for the top and bottom half
 - Supports different sizes for top and bottom half
 - Overall image size and background can be customized
 - Transparent backgrounds
 - Support for SoundCloud style bars
 - Several built-in rendering styles

## WaveFormRenderer

The `WaveFormRenderer` class allows easy rendering of files. We need to create some configuration options first.

The peak provider decides how peaks are calculated. There are four built in options you can choose from. `MaxPeakProvider` simply picks out the maximum sample value in the timeblock that each bar represents. `RmsPeakProvider` calculates the root mean square of each sample and returns the maximum value found in a specified blcok. The `SamplingPeakProvider` simply samples the samples, and you pass in a sample interval.Finally the `AveragePeakProvider` averages the sample values and takes a scale parameter to multiply the average by as it tends to produce lower values.

```c#
var maxPeakProvider = new MaxPeakProvider();
var rmsPeakProvider = new RmsPeakProvider(blockSize); // e.g. 200
var samplingPeakProvider = new SamplingPeakProvider(sampleInterval); // e.g. 200
var averagePeakProvider = new AveragePeakProvider(scaleFactor); // e.g. 4
```

Next we need to provide the rendering settings. This is an instance of `WaveFormRendererSettings` which specifies:

- **Width** - the width of the rendered image in pixels
- **TopHeight** - height of the top half of the waveform in pixels
- **BottomHeight** - height of the bottom half of the waveform in pixels. Normally set to the same as `TopHeight` but can be 0 or smaller for asymmetric waveforms
- **PixelsPerPeak** - allows for wider bars to represent each peak. Usually set to 1.
- **SpacerPixels** - allows blank spaces to be inserted between vertical bars. Usually 0 unless when wide bars are used.
- **TopPeakPen** - Pen to draw the top bars with
- **TopSpacerPen** - Pen to draw the top spacer bars with
- **BottomPeakPen** - Pen to draw the bottom bars with
- **BottomSpacerPen** - Pen to draw the bottom spacer bars with
- **DecibelScale** - if true, convert values to decibels for a logarithmic waveform
- **BackgroundColor** - background color (used if no `BackgroundImage` is specified)
- **BackgroundImage** - background image (alternative to solid color)

To simplify setting up an instance of `WaveFormRendererSettings` several derived types are supplied including 
`StandardWaveFormRendererSettings`, `SoundCloudOriginalSettings` and `SoundCloudBlockWaveFormSettings`. The latter two mimic rendering styles that have been used by SoundCloud in the past.

```c#
var myRendererSettings = new StandardWaveFormRendererSettings();
myRendererSettings.Width = 640;
myRendererSettings.TopHeight = 32;
myRendererSettings.BottomHeight = 32;
```

Now we just need to create our `WaveFormRenderer` and give it a path to the file we want to render, and pass in the peak provider we've chosen and the renderer settings:

```C#
var renderer = new WaveFormRenderer();
var audioFilePath = "myfile.mp3";
var image = renderer.Render(audioFilePath, myPeakProvider, myRendererSettings);
```

With that image we could render it to a WinForms picturebox:
```c#
pictureBox1.Image = image;
```

Or we could save it to a PNG file which you'd want to do if you were rendering on a web server for example:
```c#
image.Save("myfile.png", ImageFormat.Png);
```

 