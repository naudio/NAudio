using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Midi 
{
	/// <summary>
	/// Represents a MIDI out device
	/// </summary>
	public class MidiOut : IDisposable 
	{
		private IntPtr hMidiOut = IntPtr.Zero;
		private bool disposed = false;

		/// <summary>
		/// Gets the number of MIDI devices available in the system
		/// </summary>
		public static int NumberOfDevices 
		{
			get 
			{
				return MidiInterop.midiOutGetNumDevs();
			}
		}
		
		/// <summary>
		/// Opens a specified MIDI out device
		/// </summary>
		/// <param name="deviceNo">The device number</param>
		public MidiOut(int deviceNo) 
		{
			// TODO: callback function
			MmException.Try(MidiInterop.midiOutOpen(out hMidiOut,deviceNo,0,0,MidiInterop.CALLBACK_NULL),"midiOutOpen");
			// TODO: check for error
		}
		
		/// <summary>
		/// Closes this MIDI out device
		/// </summary>
		public void Close() 
		{
			Dispose();
		}

		/// <summary>
		/// Closes this MIDI out device
		/// </summary>
		public void Dispose() 
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets or sets the volume for this MIDI out device
		/// </summary>
		public int Volume 
		{
			// TODO: Volume can be accessed by device ID
			get 
			{
				int volume = 0;
				MmException.Try(MidiInterop.midiOutGetVolume(hMidiOut,ref volume),"midiOutGetVolume");
				return volume;
			}
			set 
			{
				MmException.Try(MidiInterop.midiOutSetVolume(hMidiOut,value),"midiOutSetVolume");
			}
		}

		/// <summary>
		/// Resets the MIDI out device
		/// </summary>
		public void Reset() 
		{
			MmException.Try(MidiInterop.midiOutReset(hMidiOut),"midiOutReset");
		}

		/// <summary>
		/// Gets the MIDI out device capabilities
		/// </summary>
		public MidiOutCapabilities Capabilities 
		{
			get 
			{
				MidiOutCapabilities caps = new MidiOutCapabilities();
				MmException.Try(MidiInterop.midiOutGetDevCaps(hMidiOut,out caps,Marshal.SizeOf(caps)),"midiOutGetDevCaps");
				return caps;
			}
		}

		/// <summary>
		/// Sends a MIDI out message
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="param1">Parameter 1</param>
		/// <param name="param2">Parameter 2</param>
		public void SendDriverMessage(int message, int param1, int param2) 
		{
			MmException.Try(MidiInterop.midiOutMessage(hMidiOut,message,param1,param2),"midiOutMessage");
		}

		/// <summary>
		/// Sends a MIDI message to the MIDI out device
		/// </summary>
		/// <param name="message">The message to send</param>
		public void Send(MidiMessage message) 
		{
			MmException.Try(MidiInterop.midiOutShortMsg(hMidiOut,message.RawData),"midiOutShortMsg");
		}
		
		/// <summary>
		/// Closes the MIDI out device
		/// </summary>
		/// <param name="disposing">True if called from Dispose</param>
		protected virtual void Dispose(bool disposing) 
		{
			if(!this.disposed) 
			{
				//if(disposing) Components.Dispose();
				MidiInterop.midiOutClose(hMidiOut);
			}
			disposed = true;         
		}

		/// <summary>
		/// Cleanup
		/// </summary>
		~MidiOut()
		{
			System.Diagnostics.Debug.Assert(false);
			Dispose(false);
		}	
	}
}
