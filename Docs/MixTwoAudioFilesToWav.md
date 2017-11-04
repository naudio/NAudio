# Mix Two Audio Files into a WAV File

In this tutorial we will mix two audio files together into a WAV file. The input files can be any supported format such as WAV or MP3.

First, we should open the two input files. We'll use `AudioFileReader` to do this.

Next, we'll use `MixingSampleProvider` to mix them together. This expects an `IEnumerable<ISampleProvider>` which 

Finally, we use `WaveFileWriter.CreateWaveFile16` passing in the `MixingSampleProvider` to mix the two files together and output a 16 bit WAV file.

```c#
using(var reader1 = new AudioFileReader("file1.wav"))
using(var reader2 = new AudioFileReader("file2.wav"))
{
    var mixer = new MixingSampleProvider(new[] { reader1, reader2 });
    WaveFileWriter.CreateWaveFile16("mixed.wav", mixer);
}
```

Note that there is potential for audio to clip. If the two files are both loud, then the combined value of a sample will be greater than 1.0f. These have to be clipped before converting back to 16 bit PCM. This can be fixed by reducing the volume of the inputs. Here's how we could set the volumes to 75% before mixing

```c#
reader1.Volume = 0.75f;
reader2.Volume = 0.75f;
```

Alternatively, if we'd used `WaveFileWriter.CreateWaveFile` instead, then the output would contain IEEE floating point samples instead of 16 bit PCM. This would result in a file twice as large, but any sample values > 1.0f would be left intact.
