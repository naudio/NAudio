using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
namespace NAudio.MediaFoundation
{
	public class MediaFoundationPlayer :IWavePlayer, IWavePosition
	{
		private IMFMediaSession m_Session;
		private Thread m_eventthread ;
		private ISimpleAudioVolume m_volume;
		private IWaveProvider m_sourcewave;
		private IMFByteStream m_sourcestream;
		private IMFClock m_clock;
		private IMFRateControl m_rate;
		public bool Prepared { get; set; } = false;
		public bool SelectAllStream { get; set; } = false;
		public event EventHandler<StoppedEventArgs> PlaybackStopped;
		public PlaybackState PlaybackState { get; private set; }

		private void ProcessEvent()
		{
			while (m_Session != null)
			{
                try { 
					m_Session.GetEvent(1, out IMFMediaEvent _event);
					_event.GetType(out MediaEventType eventtype);
					switch (eventtype)
					{
						case MediaEventType.MESessionStopped:
							PlaybackState = PlaybackState.Stopped;
							PlaybackStopped(this, new StoppedEventArgs());
							break;
						case MediaEventType.MESessionTopologyStatus:
							Guid guidManager = typeof(IAudioSessionManager).GUID;
							(new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator).
								GetDefaultAudioEndpoint(CoreAudioApi.DataFlow.Render, CoreAudioApi.Role.Multimedia, out IMMDevice endoint);
							endoint.Activate(ref guidManager, ClsCtx.ALL, IntPtr.Zero, out object _manager);
							IAudioSessionManager manager = _manager as IAudioSessionManager;
							manager.GetSimpleAudioVolume(Guid.Empty, 0, out m_volume);

							m_Session.GetClock(out m_clock);

							Guid guid_ratecontrol = typeof(IMFRateControl).GUID;
							Guid MF_RATE_CONTROL_SERVICE = Guid.Parse("866fa297-b802-4bf8-9dc9-5e3b6a9f53c9");
							MediaFoundationInterop.MFGetService(m_Session, ref MF_RATE_CONTROL_SERVICE, ref guid_ratecontrol, out object _control);
							m_rate = _control as IMFRateControl;
							Prepared = true;
							break;
					}

				}
                catch (COMException e)
                {
					if (e.HResult == MediaFoundationErrors.MF_E_NO_EVENTS_AVAILABLE)
						continue;
					else
						throw e;
                }
			}
		}
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

		public void Init(IWaveProvider waveProvider)
		{
			m_sourcewave= waveProvider;
			MediaFoundationInterop.MFCreateTempFile(MF_FILE_ACCESSMODE.MF_ACCESSMODE_READWRITE,
				MF_FILE_OPENMODE.MF_OPENMODE_DELETE_IF_EXIST, MF_FILE_FLAGS.MF_FILEFLAGS_NONE, out m_sourcestream);
			int readcount;
			do
			{
				byte[] _data = new byte[int.MaxValue];
				readcount = waveProvider.Read(_data, 0, _data.Length);
				m_sourcestream.Write(ref _data,_data.Length,out int hasread);
			} while (readcount>= int.MaxValue);
			MediaFoundationInterop.MFCreateSourceResolver(out IMFSourceResolver resolver);
			resolver.CreateObjectFromByteStream(m_sourcestream, "",SourceResolverFlags.MF_RESOLUTION_MEDIASOURCE
				|SourceResolverFlags.MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE, null, out MF_OBJECT_TYPE type, out object _source);
			IMFMediaSource source = _source as IMFMediaSource;
			source.CreatePresentationDescriptor(out IMFPresentationDescriptor descriptor);
			if (MediaFoundationInterop.MFRequireProtectedEnvironment(descriptor) == 0)
			{
				throw new ArgumentException("The data in waveProvider is protected.");
			}
			MediaFoundationInterop.MFCreateTopology(out IMFTopology topo);
			descriptor.GetStreamDescriptorCount(out uint sdcount);
			for (uint i = 0; i < sdcount; i++)
			{
				descriptor.GetStreamDescriptorByIndex(i, out bool IsSelected, out IMFStreamDescriptor sd);
				if ((!IsSelected) & SelectAllStream)
					descriptor.SelectStream(i);
				sd.GetMediaTypeHandler(out IMFMediaTypeHandler typeHandler);
				typeHandler.GetMajorType(out Guid streamtype);
				IMFActivate renderer;
				if (streamtype == MediaTypes.MFMediaType_Audio)
				{
					MediaFoundationInterop.MFCreateAudioRendererActivate(out renderer);
				}
				else
				{
					continue;
				}
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
				m_volume.SetMasterVolume(value, Guid.Empty);
            }
        }

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
			PlaybackStopped(this, new StoppedEventArgs());
		}
		public void Play()
		{
			if (m_Session == null) throw new InvalidOperationException("This player hasn't initialized yet");
			if (!Prepared) throw new InvalidOperationException("This player is still loading.");
			PropVariant prop = new PropVariant() { 
			vt = 0//VT_EMPTY
			};
			m_Session.Start(Guid.Empty, ref prop);
			PlaybackState = PlaybackState.Playing;
		}
		public long GetPosition()
        {
			if(m_Session==null) throw new InvalidOperationException("This player hasn't initialized yet");
			if (!Prepared) throw new InvalidOperationException("This player is still loading.");
			m_clock.GetProperties(out MFCLOCK_PROPERTIES clockprop);			
			switch (PlaybackState)
			{
				case PlaybackState.Stopped:
					return 0;
				default:
                    m_clock.GetCorrelatedTime(0, out long timeofclock, out _);
					double time = timeofclock * ((double)1 / clockprop.qwClockFrequency);
					return (long)(time * m_sourcewave.WaveFormat.AverageBytesPerSecond);
			}
		}
		public void Dispose()
		{
			if (m_Session != null) 
			{
				m_Session.Shutdown();
				m_Session = null; 
			}
			GC.SuppressFinalize(this);
		}
	}
}