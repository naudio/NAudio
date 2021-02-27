using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NAudio.MediaFoundation
{

    public class MediaFounationSimplePlayer
    {
        private Thread m_Eventthread;
        private ISimpleAudioVolume m_Volume;
        private IMFMediaSession m_Session;
        private readonly List<WaveFormat> m_format = new List<WaveFormat>();
        private string m_Url;
        private IMFPresentationDescriptor m_pDescriptor;
        private IMFPresentationClock m_Clock;
        private IMFRateControl m_Rate;
        private long m_Duration;
        private int m_streamcount;
        private StreamSelectFlags m_selectflag;
        private List<bool> m_DefaultStreamSelect = new List<bool>();

        public bool IsPrepared { get; private set; }
        /// <summary>
        /// How the streams in the audio will be selected.
        /// </summary>
        public StreamSelectFlags StreamSelectFlag {
            get {
                return m_selectflag;
            }
            set {
                m_selectflag = value;
                UpdateSelectFlag();
            }
        }
        /// <summary>
        /// A list of formats of all streams.
        /// </summary>
        public IReadOnlyCollection<WaveFormat> Formats
        {
            get
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                return m_format;
            }
        }
        /// <summary>
        /// The duration of the audio.
        /// </summary>
        public long Duration { 
            get {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                return m_Duration;
            } 
            private set {
                m_Duration = value;
            } 
        }
        /// <summary>
        /// The count of streams.
        /// </summary>
        public int StreamCount { 
            get {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                return m_streamcount;
            } 
            private set {
                m_streamcount = value;
            } 
        }
        /// <summary>
        /// The filename of the audio.
        /// </summary>
        public string URL {
            get {
                return m_Url;
            }
            set
            {
                if (!File.Exists(value)) throw new FileNotFoundException("This file doesn't exist");
                string _url = m_Url;
                m_Url = value;
                try
                {
                    Load();
                }
                catch (Exception e)
                {
                    m_Url = _url;
                    throw e;
                }
            }
        }
        /// <summary>
        /// The playback volume.
        /// </summary>
        public float Volume
        {
            get
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                m_Volume.GetMasterVolume(out float volume);
                return volume;
            }
            set
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                if (value > 1 | value < 0) throw new ArgumentException("The value is out of range");
                m_Volume.SetMasterVolume(value, Guid.Empty);
            }
        }
        /// <summary>
        /// Is the playback muted.
        /// </summary>
        public bool Muted
        {
            get
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                m_Volume.GetMute(out bool muted);
                return muted;
            }
            set
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                m_Volume.SetMute(value, Guid.Empty);
            }
        }
        /// <summary>
        /// The playback rate.
        /// Postive values indicate forward playback, negative values indicate reverse playback, and zero indicates stopping.
        /// </summary>
        public float Rate
        {
            get
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                m_Rate.GetRate(out _, out float rate);
                return rate;
            }
            set
            {
                if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
                if (State == PlaybackState.Playing) throw new InvalidOperationException("Can't change rate while playing.");
                if (State == PlaybackState.Paused & value * Rate < 0) throw new InvalidOperationException("Can't change playback direction while paused.");
                if (State == PlaybackState.Paused & ((value ==0&Rate<0)|(value<0&Rate==0))) throw new InvalidOperationException("Can't switch between reserve playback and stopping paused.");
                try
                {
                    m_Rate.SetRate(false, value);
                }
                catch(COMException)
                {
                    throw new ArgumentOutOfRangeException("Unsupport rate.");
                }
            }
        }
        /// <summary>
        /// The state of the player.
        /// </summary>
        public PlaybackState State { get; private set; } = PlaybackState.Stopped;
        /// <summary>
        /// Playback ended.
        /// </summary>
        public event EventHandler<StoppedEventArgs> Ended;
        /// <summary>
        /// Prepared to play.
        /// </summary>
        public event EventHandler<EventArgs> Prepared;
        /// <summary>
        /// Playback started.
        /// </summary>
        public event EventHandler<EventArgs> Started;
        /// <summary>
        /// Playback paused.
        /// </summary>
        public event EventHandler<PausedEventArgs> Paused;
        /// <summary>
        /// Playback stopped.
        /// </summary>
        public event EventHandler<EventArgs> Stopped;

        private void UpdateSelectFlag()
        {
            for (uint i = 0; i < m_streamcount; i++)
            {
                m_pDescriptor.GetStreamDescriptorByIndex(i, out bool IsSelected, out _);
                switch (StreamSelectFlag)
                {
                    case StreamSelectFlags.SelectAllStream:
                        if (!IsSelected) m_pDescriptor.SelectStream(i);
                        break;
                    case StreamSelectFlags.SelectNone:
                        if (IsSelected) m_pDescriptor.DeselectStream(i);
                        break;
                    case StreamSelectFlags.SelectByDefault:
                        if (m_DefaultStreamSelect[(int)i]) m_pDescriptor.SelectStream(i);
                        else m_pDescriptor.DeselectStream(i);
                        break;
                }
            }
        }
        private void ProcessEvent()
        {
            while (m_Session != null)
            {
                try
                {
                    m_Session.GetEvent(1, out IMFMediaEvent _event);//requests events and returns immediately
                    _event.GetType(out MediaEventType eventtype);
                    switch (eventtype)
                    {
                        case MediaEventType.MESessionEnded:
                            State = PlaybackState.Stopped;
                            Ended?.Invoke(this, new StoppedEventArgs());
                            break;
                        case MediaEventType.MESessionPaused:
                            Paused?.Invoke(this, new PausedEventArgs(GetPosition()));
                            break;
                        case MediaEventType.MESessionStopped:
                            Stopped?.Invoke(this, new StoppedEventArgs());
                            break;
                        case MediaEventType.MESessionStarted:
                            Started.Invoke(this, new EventArgs());
                            break;
                        case MediaEventType.MESessionTopologyStatus://topology loaded
                            Guid guidManager = typeof(IAudioSessionManager).GUID;
                            (new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator).
                                GetDefaultAudioEndpoint(CoreAudioApi.DataFlow.Render, CoreAudioApi.Role.Multimedia, out IMMDevice endoint);
                            endoint.Activate(ref guidManager, ClsCtx.ALL, IntPtr.Zero, out object _manager);
                            IAudioSessionManager manager = _manager as IAudioSessionManager;
                            manager.GetSimpleAudioVolume(Guid.Empty, 0, out m_Volume);

                            m_Session.GetClock(out m_Clock);

                            Guid guid_ratecontrol = typeof(IMFRateControl).GUID;
                            Guid MF_RATE_CONTROL_SERVICE = Guid.Parse("866fa297-b802-4bf8-9dc9-5e3b6a9f53c9");
                            MediaFoundationInterop.MFGetService(m_Session, ref MF_RATE_CONTROL_SERVICE, ref guid_ratecontrol, out object _control);//gets rate control
                            m_Rate = _control as IMFRateControl;
                            IsPrepared = true;

                            Prepared?.Invoke(this, new EventArgs());
                            
                            break;
                    }
                    _event = null;

                }
                catch (COMException e)
                {
                    if (e.HResult == MediaFoundationErrors.MF_E_NO_EVENTS_AVAILABLE)
                        continue;
                    else
                        throw e;
                }
                catch (ThreadAbortException)
                {
                    break;
                }
            }
        }
        private void Load()
        {           
            MediaFoundationInterop.MFCreateSourceResolver(out IMFSourceResolver resolver);
            object unknown;
            try
            {
                resolver.CreateObjectFromURL(URL, SourceResolverFlags.MF_RESOLUTION_MEDIASOURCE | SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE,
                    null, out _, out unknown);
            }
            catch
            {
                throw new ArgumentException("Unsupported type.");
            }
            MediaFoundationInterop.MFCreateMediaSession(IntPtr.Zero, out m_Session);
            MediaFoundationInterop.MFCreateTopology(out IMFTopology topo);
            IMFMediaSource source = unknown as IMFMediaSource;
            source.CreatePresentationDescriptor(out m_pDescriptor);
            m_pDescriptor.GetUINT64(MediaFoundationAttributes.MF_PD_DURATION, out long dur);
            m_Duration  = dur / 10000000;
            m_pDescriptor.GetStreamDescriptorCount(out uint sdcount);
            m_streamcount = (int)sdcount;
            for (uint i = 0; i < m_streamcount; i++)
            {
                m_pDescriptor.GetStreamDescriptorByIndex(i, out bool IsSelected, out IMFStreamDescriptor sd);
                m_DefaultStreamSelect.Add(IsSelected);
                switch (StreamSelectFlag)
                {
                    case StreamSelectFlags.SelectAllStream:
                        if (!IsSelected) m_pDescriptor.SelectStream(i);
                        break;
                    case StreamSelectFlags.SelectNone:
                        if (IsSelected) m_pDescriptor.DeselectStream(i);
                        break;
                }
                sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
                typeHandler.GetMediaTypeByIndex(0, out IMFMediaType mediaType);
                mediaType.GetMajorType(out Guid streamtype);
                IMFActivate renderer;
                if (streamtype == MediaTypes.MFMediaType_Audio)
                {
                    MediaFoundationInterop.MFCreateAudioRendererActivate(out renderer);
                    mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND, out int rate);//SampleRate
                    mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_NUM_CHANNELS, out int channelcount);
                    int samplesize;
                    try
                    {
                        mediaType.GetUINT32(MediaFoundationAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE, out samplesize);
                    }
                    catch (COMException e)
                    {
                        if ((uint)e.HResult != 0xC00D36E6)
                            throw e;
                        else
                            samplesize = 8;
                    }
                    m_format.Add(new WaveFormat(rate, samplesize, channelcount));
                }
                else
                {
                    continue;
                }
                MediaFoundationInterop.MFCreateTopologyNode(MF_TOPOLOGY_TYPE.MF_TOPOLOGY_SOURCESTREAM_NODE, out IMFTopologyNode sourcenode);
                sourcenode.SetUnknown(MediaFoundationAttributes.MF_TOPONODE_SOURCE, source);
                sourcenode.SetUnknown(MediaFoundationAttributes.MF_TOPONODE_PRESENTATION_DESCRIPTOR, m_pDescriptor);
                sourcenode.SetUnknown(MediaFoundationAttributes.MF_TOPONODE_STREAM_DESCRIPTOR, sd);
                topo.AddNode(sourcenode);
                MediaFoundationInterop.MFCreateTopologyNode(MF_TOPOLOGY_TYPE.MF_TOPOLOGY_OUTPUT_NODE, out IMFTopologyNode outputnode);
                outputnode.SetObject(renderer);
                topo.AddNode(outputnode);
                sourcenode.ConnectOutput(0, outputnode, 0);
            }
            m_Session.SetTopology(0, topo);
            m_Eventthread = new Thread(ProcessEvent);
            m_Eventthread.Start();
        }
        /// <summary>
        /// Constructs a new MediaFounationSimplePlayer.
        /// </summary>
        /// <param name="url">The file to play.</param>
        /// <param name="m_selectFlags">Specify how the player selects streams.</param>
        public MediaFounationSimplePlayer(string url, StreamSelectFlags selectFlags = StreamSelectFlags.SelectByDefault)
        {
            if (!File.Exists(url)) throw new FileNotFoundException("This file doesn't exist.");
            MediaFoundationApi.Startup();
            m_selectflag= selectFlags;
            URL = url;
            Load();
        }
        /// <summary>
        /// Selects a stream in the audio.
        /// </summary>
        /// <param name="index">The index of stream.</param>
        public void SelectStream(uint index)
        {
            if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
            if (StreamSelectFlag != StreamSelectFlags.SelectCustom) throw new InvalidOperationException("The StreamSelectFlag isn't Custom.");
            if (index >= StreamCount) throw new ArgumentException("Out of valid range.");
            m_pDescriptor.SelectStream(index);
        }
        /// <summary>
        /// Starts playback.
        /// </summary>
        /// <param name="StartedPosition">Where to start playback,in second.</param>
        public void Play(long StartedPosition)
        {
            if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
            if (StartedPosition > Duration) throw new ArgumentException("Invaalid time.");
            PropVariant pos = new PropVariant
            {
                vt = (short)VarEnum.VT_I8,
                hVal = StartedPosition * 10000000,
            };
            m_Session.Start(Guid.Empty, pos);
        }
        /// <summary>
        /// Resumes playback.
        /// </summary>
        public void Resume()
        {
            if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
            PropVariant pos = new PropVariant
            {
                vt = (short)VarEnum.VT_EMPTY,
            };
            m_Session.Start(Guid.Empty, pos);
        }
        /// <summary>
        /// Pauses playback.
        /// </summary>
        public void Pause()
        {
            if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
            if (State != PlaybackState.Playing) throw new InvalidOperationException("The player isn't playing");
            m_Session.Pause();
        }
        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop()
        {
            if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
            if (State != PlaybackState.Playing) throw new InvalidOperationException("The player isn't playing");
            m_Session.Stop();
        }
        /// <summary>
        /// Closes the player
        /// </summary>
        public void Close()
        {
            Marshal.FinalReleaseComObject(m_Clock);
            Marshal.FinalReleaseComObject(m_pDescriptor);
            Marshal.FinalReleaseComObject(m_Rate);
            Marshal.FinalReleaseComObject(m_Volume);
            m_Session.Shutdown();
            m_Session = null;
            m_Eventthread.Join();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Gets the position where the player has played.
        /// </summary>
        /// <returns></returns>
        public long GetPosition()
        {
            if (!IsPrepared) throw new InvalidOperationException("This player is still loading.");
            m_Clock.GetTime(out long time);
            return time / 10000000;
        }
    }
    /// <summary>
    /// How the player selects streams.
    /// </summary>
    public enum StreamSelectFlags
    {
        /// <summary>
        /// Select all streams in the audio.
        /// </summary>
        SelectAllStream,
        /// <summary>
        /// Select the streams as default.
        /// </summary>
        SelectByDefault,
        /// <summary>
        /// Deselect all streams in the audio
        /// </summary>
        SelectNone,
        /// <summary>
        /// Select the streams as default and the user can select the streams as wanted.
        /// </summary>
        SelectCustom
    }
    public class PausedEventArgs : EventArgs
    {
        /// <summary>
        /// Where the player has played while pausing.
        /// </summary>
        public long StopPosition { get; private set; }
        public PausedEventArgs(long stopposition)
        {
            StopPosition = stopposition;
        }
    }
}
