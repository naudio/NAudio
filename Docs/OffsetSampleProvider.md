# Using OffsetSampleProvider

`OffsetSampleProvider` allows you to extract a sub-section of another `ISampleProvider`. You can skip over the start of the source `ISampleProvider` with `SkipOver` and limit how much audio you play from the source with `Take`. You can also insert leading and trailing silence with `DelayBy` and `LeadOut`. 

`Take` is especially useful when working with never-ending `ISampleProvider` sources such as `SignalGenerator`. 

Let's look at an example. Here, the `OffsetSampleProvider` uses a `SignalGenerator` as its source. It inserts 1 second of silence before playing for 5 seconds and then inserts 1 extra second of silence at the end:

```c#
// the source ISampleProvider
var sineWave = new SignalGenerator() { 
    Gain = 0.2, 
    Frequency = 500,
    Type = SignalGeneratorType.Sin};
var trimmed = new OffsetSampleProvider(sineWave) {
    DelayBy = TimeSpan.FromSeconds(1),
    Take = TimeSpan.FromSeconds(5),
    LeadOut = TimeSpan.FromSeconds(1)
};
```

For another example, let's say we have an audio file and we want to skip over the first one minute, and then take a 30 second excerpt and write it to a WAV file:

```c#
var source = new AudioFileReader("example.mp3");
var trimmed = new OffsetSampleProvider(source) {
    SkipOver = TimeSpan.FromSeconds(30),
    Take = TimeSpan.FromSeconds(60),
WaveFileWriter.CreateWaveFile16(outputFilePath, trimmed);
```

## Skip and Take Extension Methods

NAudio also offers some helpful extension methods to simplify the above task. Skip and Take are extension methods on `ISampleProvider` and create an `OffsetSampleProvider` behind the scenes. So the previous example could be rewritten:

```c#
var trimmed = new AudioFileReader("example.mp3")
                   .Skip(TimeSpan.FromSeconds(30))
                   .Take(TimeSpan.FromSeconds(60));
WaveFileWriter.CreateWaveFile16(outputFilePath, trimmed);
```

## Optimizing SkipOver

Note that `SkipOver` is implemented by simply reading that amount of audio from the source and discarding it. Obviously if the source is a file as in this example, it would be more efficient just to position it to the desired starting point:

```c#
var source = new AudioFileReader("example.mp3");
source.CurrentTime = TimeSpan.FromSeconds(30);
var trimmed = source.Take(TimeSpan.FromSeconds(60));
WaveFileWriter.CreateWaveFile16(outputFilePath, trimmed);
```


## Sample Accurate Trimming

As well as the TimeSpan based versions of the `SkipOver`, `DelayBy` `Take` and `LeadOut` properties, there are sample based ones, for when you need accurate control over exactly how many samples of audio to skip and take. These are called `SkipOverSamples`, `DelayBySamples`, `TakeSamples` and `LeadOutSamples`. They're calculated automatically for you when you use the `TimeSpan` based properties, but you can set them directly yourself.
