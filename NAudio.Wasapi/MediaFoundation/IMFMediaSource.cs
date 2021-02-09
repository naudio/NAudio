using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
	/// <summary>
	/// IMFMediaSource interface
	/// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfmediasource
	/// </summary>
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true), ComImport, Guid("279a808d-aec7-40c8-9c6b-a6b492c78a66")]
	public interface IMFMediaSource
	{
		/// <summary>
		/// Retrieves the next event in the queue. This method is synchronous.
		/// </summary>
		void GetEvent(uint dwFlags, out IMFMediaEvent ppEvent);
		/// <summary>
		/// Begins an asynchronous request for the next event in the queue.
		/// </summary>
		void BeginGetEvent(IntPtr pCallback, IntPtr punkState);
		/// <summary>
		/// Completes an asynchronous request for the next event in the queue.
		/// </summary>
		void EndGetEvent(IntPtr pResult, out IMFMediaEvent ppEvent);
		/// <summary>
		/// Puts a new event in the object's queue.
		/// </summary>
		void QueueEvent(uint met, ref Guid guidExtendedType, int hrStatus, ref PropVariant pvValue);
		/// <summary>
		/// Retrieves the characteristics of the media source.
		/// </summary>
		void GetCharacteristics(out MFMEDIASOURCE_CHARACTERISTICS pdwCharacteristics);
		/// <summary>
		/// Retrieves a copy of the media source's presentation descriptor. 
		/// Applications use the presentation descriptor to select streams and to get information about the source content.
		/// </summary>
		void CreatePresentationDescriptor(out IMFPresentationDescriptor ppPresentationDescriptor);
		/// <summary>
		/// Starts, seeks, or restarts the media source by specifying where to start playback.
		/// </summary>
		void Start(IMFPresentationDescriptor pPresentationDescriptor, ref Guid pguidTimeFormat, ref PropVariant pvarStartPosition);
		/// <summary>
		/// Stops all active streams in the media source.
		/// </summary>
		void Stop();
		/// <summary>
		/// Pauses all active streams in the media source.
		/// </summary>
		void Pause();
		/// <summary>
		/// Shuts down the media source and releases the resources it is using.
		/// </summary>
		void Shutdown();
	}
}