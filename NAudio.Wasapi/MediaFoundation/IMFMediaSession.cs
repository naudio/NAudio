using System;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.MediaFoundation
{
	/// <summary>
	/// IMFMediaSession interface
	/// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfmediasession
	/// </summary>
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true), ComImport, Guid("90377834-21D0-4dee-8214-BA2E3E6C1127")]
	public interface IMFMediaSession
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
		/// Sets a topology on the Media Session.
		/// </summary>
		void SetTopology(uint dwSetTopologyFlags, IMFTopology pTopology);
		/// <summary>
		/// Clears all of the presentations that are queued for playback in the Media Session.
		/// </summary>
		void ClearTopologies();
		/// <summary>
		/// Starts the Media Session.
		/// </summary>
		void Start(ref Guid pguidTimeFormat, ref PropVariant pvarStartPosition);
		/// <summary>
		/// Pauses the Media Session.
		/// </summary>
		void Pause();
		/// <summary>
		/// Stops the Media Session.
		/// </summary>
		void Stop();
		/// <summary>
		/// Closes the Media Session and releases all of the resources it is using.
		/// </summary>
		void Close();
		/// <summary>
		/// Shuts down the Media Session and releases all the resources used by the Media Session.
		/// </summary>
		void Shutdown();
		/// <summary>
		/// Retrieves the Media Session's presentation clock.
		/// </summary>
		void GetClock([MarshalAs(UnmanagedType.IUnknown)]out object ppClock);
		/// <summary>
		/// Retrieves the capabilities of the Media Session, based on the current presentation.
		/// </summary>	
		void GetSessionCapabilities(out uint pdwCaps);
		/// <summary>
		/// Gets a topology from the Media Session.
		/// </summary>
		void GetFullTopology(uint dwGetFullTopologyFlags, ulong TopoId, IMFTopology ppFullTopology);
	}
}