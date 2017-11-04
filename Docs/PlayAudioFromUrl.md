# Play Audio From URL

The `MediaFoundationReader` class provides the capability of playing audio directly from a URL and supports many common audio file formats such as MP3.

In this example designed to be run from a console app, we use `MediaFoundationReader` to load the audio from the network and then simply block until playback has finished.

```c#
var url = "http://media.ch9.ms/ch9/2876/fd36ef30-cfd2-4558-8412-3cf7a0852876/AzureWebJobs103.mp3";
using(var mf = new MediaFoundationReader(url))
using(var wo = new WaveOutEvent())
{
    wo.Init(mf);
    wo.Play();
    while (wo.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(1000);
    }
}
```