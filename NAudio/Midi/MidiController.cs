using System;

namespace NAudio.Midi
{
	/// <summary>
	/// MidiController enumeration
	/// </summary>
	public enum MidiController : byte 
	{
		/// <summary>Modulation</summary>
		Modulation = 1,
		/// <summary>Main volume</summary>
		MainVolume = 7,
		/// <summary>Pan</summary>
		Pan = 10,
		/// <summary>Expression</summary>
		Expression = 11,
		/// <summary>Sustain</summary>
		Sustain = 64,
		/// <summary>Reset all controllers</summary>
		ResetAllControllers = 121,
		/// <summary>All notes off</summary>
		AllNotesOff = 123,
	}
}
