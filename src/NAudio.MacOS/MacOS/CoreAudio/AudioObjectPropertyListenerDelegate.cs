namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Provides the signature that all the HAL events are using for dispatching various important events.
/// </summary>
/// <param name="audioObject">The <see cref="AudioObject"/> from which this event is fired from.</param>
public delegate void AudioObjectPropertyListenerDelegate(AudioObject audioObject);