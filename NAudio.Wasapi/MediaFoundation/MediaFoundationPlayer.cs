using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
namespace NAudio.MediaFoundation
{
    public class MediaFoundationPlayer :IWavePlayer, IWavePosition
    {
        private IMFMediaSession m_Session;
        private Thread m_eventthread ;
        private ISimpleAudioVolume m_volume;
        private IWaveProvider m_sourcewave;
        private IMFPresentationClock m_clock;
        private IMFRateControl m_rate;
        /// <summary>
        /// Is media session and topology loaded.
        /// </summary>
        public bool Prepared { get; set; } = false;
        /// <summary>
        /// Whether to select al stream in the provider.
        /// </summary>
        public bool SelectAllStream { get; set; } = false;
        public event EventHandler<StoppedEventArgs> PlaybackStopped;
        public PlaybackState PlaybackState { get; private set; }
        /// <summary>
        /// Processes the media session event.
        /// </summary>
        private void ProcessEvent()
        {
            while (m_Session != null)
            {
                try { 
                    m_Session.GetEvent(1, out IMFMediaEvent _event);//requests events and returns immediately
                    _event.GetType(out MediaEventType eventtype);
                    switch (eventtype)
                    {
                        case MediaEventType.MESessionEnded :
                            PlaybackState = PlaybackState.Stopped;
                            PlaybackStopped?.Invoke(this, new StoppedEventArgs());
                            break;
                        case MediaEventType.MESessionTopologyStatus://topology loaded
                            Guid guidManager = typeof(IAudioSessionManager).GUID;
                            (new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator).
                                GetDefaultAudioEndpoint(CoreAudioApi.DataFlow.Render, CoreAudioApi.Role.Multimedia, out IMMDevice endoint);
                            endoint.Activate(ref guidManager, ClsCtx.ALL, IntPtr.Zero, out object _manager);
                            IAudioSessionManager manager = _manager as IAudioSessionManager;
                            manager.GetSimpleAudioVolume(Guid.Empty, 0, out m_volume);

                            m_Session.GetClock(out m_clock);

                            Guid guid_ratecontrol = typeof(IMFRateControl).GUID;
                            Guid MF_RATE_CONTROL_SERVICE = Guid.Parse("866fa297-b802-4bf8-9dc9-5e3b6a9f53c9");
                            MediaFoundationInterop.MFGetService(m_Session, ref MF_RATE_CONTROL_SERVICE, ref guid_ratecontrol, out object _control);//gets rate control
                            m_rate = _control as IMFRateControl;
                            Prepared = true;
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
        /// <summary>
        /// The audio format.
        /// </summary>
        public WaveFormat OutputWaveFormat
        {
            get
            {
                try
                {
                    return m_sourcewave.WaveFormat;
                }
                catch (NullReferenceException)
                {
                    throw new InvalidOperationException("This player hasn't initialized yet");
                }
            }
        }
        /// <summary>
        /// Loads IWaveProvider.
        /// </summary>
        /// <param name="waveProvider">The waveProvider to be loaded.</param>
        public void Init(IWaveProvider waveProvider)
        {
            MediaFoundationApi.Startup();
            m_sourcewave = waveProvider;
            int readcount;
            MemoryStream msByteStrem = new MemoryStream();
            byte[] _data;
            do
            {
                readcount = 0;
                _data = new byte[1000000000];
                readcount = waveProvider.Read(_data, 0, _data.Length);
                if (readcount < 0)
                    continue;
                msByteStrem.Write(_data, 0, readcount);
            } while (readcount >=  _data.Length|readcount<0);//Creates a IMFByteStream and fills it with the data in waveProvider.
            ComStream csByteStream = new ComStream(msByteStrem);
            IMFByteStream mfByteStream = MediaFoundationApi.CreateByteStream(csByteStream);
            MediaFoundationInterop.MFCreateSourceResolver(out IMFSourceResolver resolver);
            IMFAttributes streamattributes = mfByteStream as IMFAttributes;
            mfByteStream.GetLength(out long _length);
            resolver.CreateObjectFromByteStream(mfByteStream, null, SourceResolverFlags.MF_RESOLUTION_MEDIASOURCE
                | SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE, null, out _, out object _source);//Turns the stream to IMFMediaSource

            IMFMediaSource source = _source as IMFMediaSource;
            source.CreatePresentationDescriptor(out IMFPresentationDescriptor descriptor);
            MediaFoundationInterop.MFCreateTopology(out IMFTopology topo);
            descriptor.GetStreamDescriptorCount(out uint sdcount);
            for (uint i = 0; i < sdcount; i++)//For each stream in the source,creates renderer and adds to the topology.
            {
                descriptor.GetStreamDescriptorByIndex(i, out bool IsSelected, out IMFStreamDescriptor sd);
                if (!IsSelected)
                {
                    if (SelectAllStream)
                        descriptor.SelectStream(i);
                    else
                        continue;
                }
                sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
                typeHandler.GetMajorType(out Guid streamtype);
                IMFActivate renderer;
                if (streamtype == MediaTypes.MFMediaType_Audio)
                {
                    MediaFoundationInterop.MFCreateAudioRendererActivate(out renderer);//Creates renderer for audio streams
                }
                else
                {
                    continue;
                }
                //Creats and equips the topology nodes of the certain stream 
                MediaFoundationInterop.MFCreateTopologyNode(MF_TOPOLOGY_TYPE.MF_TOPOLOGY_SOURCESTREAM_NODE, out IMFTopologyNode sourcenode);
                sourcenode.SetUnknown(MediaFoundationAttributes.MF_TOPONODE_SOURCE, source);
                sourcenode.SetUnknown(MediaFoundationAttributes.MF_TOPONODE_PRESENTATION_DESCRIPTOR, descriptor);
                sourcenode.SetUnknown(MediaFoundationAttributes.MF_TOPONODE_STREAM_DESCRIPTOR, sd);
                topo.AddNode(sourcenode);
                MediaFoundationInterop.MFCreateTopologyNode(MF_TOPOLOGY_TYPE.MF_TOPOLOGY_OUTPUT_NODE, out IMFTopologyNode outputnode);
                outputnode.SetObject(renderer);
                topo.AddNode(outputnode);
                sourcenode.ConnectOutput(0, outputnode, 0);
            }
            MediaFoundationInterop.MFCreateMediaSession(IntPtr.Zero, out m_Session);
            m_Session.SetTopology(0, topo);
            m_eventthread = new Thread(ProcessEvent);
            m_eventthread.Start();
        }
        /// <summary>
        /// Playback volume.
        /// </summary>
        public float Volume
        {
            get
            {
                if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
                if (!Prepared) throw new InvalidOperationException("This player is still loading.");
                m_volume.GetMasterVolume(out float volvalue);
                return volvalue;
            }
            set
            {
                if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
                if (!Prepared) throw new InvalidOperationException("This player is still loading.");
                if (value > 1 | value < 0) throw new ArgumentException("The value is out of range");
                m_volume.SetMasterVolume(value, Guid.Empty);
            }
        }
        /// <summary>
        /// Playback rate
        /// </summary>
        public float Rate
        {
            get
            {
                if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
                if (!Prepared) throw new InvalidOperationException("This player is still loading.");
                m_rate.GetRate(out _, out float _rate);
                return _rate;
            }
            set
            {
                if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
                if (!Prepared) throw new InvalidOperationException("This player is still loading.");
                m_rate.SetRate(false, value);
            }
        }
        public void Pause()
        {
            if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
            if (!Prepared) throw new InvalidOperationException("This player is still loading.");
            m_Session.Pause();
            PlaybackState = PlaybackState.Paused;
        }
        public void Stop()
        {
            if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
            if (!Prepared) throw new InvalidOperationException("This player is still loading.");
            m_Session.Stop();
            PlaybackState = PlaybackState.Stopped;
            PlaybackStopped?.Invoke(this, new StoppedEventArgs());
        }
        public void PlayFromBegining()
        {
            if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
            if (!Prepared) throw new InvalidOperationException("This player is still loading.");
            PropVariant pos = new PropVariant()
            {
                vt = (short)VarEnum.VT_I8,
                hVal=0
            };
            m_Session.Start(Guid.Empty, ref pos);
            PlaybackState = PlaybackState.Playing;
        }
        public void Play()
        {
            if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
            if (!Prepared) throw new InvalidOperationException("This player is still loading.");
            PropVariant pos = new PropVariant()
            {
                vt = (short)VarEnum.VT_EMPTY,
            };
            m_Session.Start(Guid.Empty, ref pos);
            PlaybackState = PlaybackState.Playing;
        }
        public long GetPosition()
        {
            if(m_Session==null) throw new InvalidOperationException("This player hasn't initialized yet");
            if (!Prepared) throw new InvalidOperationException("This player is still loading.");
            switch (PlaybackState)
            {
                case PlaybackState.Stopped:
                    return 0;
                default:
                    m_clock.GetTime(out long _time);
                    long timeinsec = _time / 10000000;
                    return m_sourcewave.WaveFormat.AverageBytesPerSecond * timeinsec;
            }
        }
        public void Dispose()
        {
            if (m_Session != null) 
            {
                m_eventthread?.Abort();
                m_Session.Shutdown();
                m_Session = null; 
            }
            GC.SuppressFinalize(this);
        }
    }
}