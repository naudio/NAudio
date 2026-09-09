namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Defines the kind of a specifiied <see cref="CoreAudio.AudioControl"/> instance.
/// </summary>
public enum AudioControlKind
{
    /// <summary>
    /// The kind of the audio control could not be determined.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The kind of the audio control is itself.
    /// </summary>
    AudioControl,
    /// <summary>
    /// The kind of the audio control is an <see cref="AudioSliderControl"/>.
    /// </summary>
    SliderControl,
    /// <summary>
    /// The kind of the audio control is an <see cref="AudioLevelControl"/>.
    /// </summary>
    LevelControl,
    /// <summary>
    /// The kind of the audio control is a volume control subclassing from the <see cref="AudioLevelControl"/> class.
    /// </summary>
    VolumeControl,
    /// <summary>
    /// The kind of the audio control is a LFE volume control subclassing from the <see cref="AudioLevelControl"/> class.
    /// </summary>
    LFEVolumeControl,
    /// <summary>
    /// The kind of the audio control is an <see cref="AudioBooleanControl"/>.
    /// </summary>
    BooleanControl,
    /// <summary>
    /// The kind of the audio control is a mute control subclassing from the <see cref="AudioBooleanControl"/> class.
    /// </summary>
    MuteControl,
    /// <summary>
    /// The kind of the audio control is a solo control subclassing from the <see cref="AudioBooleanControl"/> class. <br />
    /// Solo control means that solo is enabled making just that element audible and the other elements inaudible.
    /// </summary>
    SoloControl,
    /// <summary>
    /// A subclass of the <see cref="AudioBooleanControl"/> class where a true value means something is plugged into that element.
    /// </summary>
    JackControl,
    /// <summary>
    /// A subclass of the <see cref="AudioBooleanControl"/> class where true means that mute is
    /// enabled making that LFE element inaudible. This control is for LFE channels
    /// that result from bass management. Note that LFE channels that are
    /// represented as normal audio channels must use an <see cref="MuteControl"/>.
    /// </summary>
    LFEMuteControl,
    /// <summary>
    /// A subclass of the <see cref="AudioBooleanControl"/> class where true means that the element's hardware has phantom power enabled.
    /// </summary>
    PhantomPowerControl,
    /// <summary>
    /// A subclass of the <see cref="AudioBooleanControl"/> class where true 
    /// means that the phase of the signal on the given element is being 
    /// inverted by 180 degrees.
    /// </summary>
    PhaseInvertControl,
    /// <summary>
    /// A subclass of the <see cref="AudioBooleanControl"/> class where true means that the signal
    /// for the element has exceeded the sample range. Once a clip light is turned
    /// on, it is to stay on until either the value of the control is set to false
    /// or the current IO session stops and a new IO session starts.
    /// </summary>
    ClipLightControl,
    /// <summary>
    /// An <see cref="AudioBooleanControl"/> where true means that the talkback channel is
    /// enabled. This control is for talkback channels that are handled outside of 
    /// the regular IO channels. If the talkback channel is among the normal IO
    /// channels, it will use AudioMuteControl.
    /// </summary>
    TalkbackControl,
    /// <summary>
    /// An <see cref="AudioBooleanControl"/> where true means that the listenback channel is
    /// audible. This control is for listenback channels that are handled outside of 
    /// the regular IO channels. If the listenback channel is among the normal IO
    /// channels, it will use AudioMuteControl.
    /// </summary>
    ListenbackControl,
    /// <summary>
    /// The kind of the audio control is an <see cref="AudioSelectorControl"/>.
    /// </summary>
    SelectorControl,
    /// <summary>
    /// A subclass of the <see cref="AudioSelectorControl"/> class that identifies where the data for the element is coming from.
    /// </summary>
    DataSourceControl,
    /// <summary>
    /// A subclass of the <see cref="AudioSelectorControl"/> class that identifies where the data for the element is going.
    /// </summary>
    DataDestinationControl,
    /// <summary>
    /// A subclass of the <see cref="AudioSelectorControl"/> class that identifies where the timing info for the object is coming from.
    /// </summary>
    ClockSourceControl,
    /// <summary>
    /// A subclass of the <see cref="AudioSelectorControl"/> class that identifies the nominal
    /// line level for the element. Note that this is not a gain stage but rather
    /// indicating the voltage standard (if any) used for the element, such as
    /// +4dBu, -10dBV, instrument, etc.
    /// </summary>
    LineLevelControl,
    /// <summary>
    /// A subclass of the <see cref="AudioSelectorControl"/> class that 
    /// indicates the setting for the high pass filter on the given element.
    /// </summary>
    HighPassFilterControl,
    /// <summary>
    /// The kind of the audio control is an <see cref="AudioStereoPanControl"/>.
    /// </summary>
    StereoPanControl
}