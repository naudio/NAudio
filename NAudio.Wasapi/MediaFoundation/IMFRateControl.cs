using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
	/// <summary>
	/// IMFRateControl interface
	/// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfratecontrol
	/// </summary>
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true), ComImport, Guid("88ddcd21-03c3-4275-91ed-55ee3929328f")]
	public interface IMFRateControl
	{
		/// <summary>
		/// Sets the playback rate.
		/// </summary>
		void SetRate(bool fThin, float flRate);
		/// <summary>
		/// Gets the current playback rate.
		/// </summary>
		void GetRate(ref bool pfThin, ref float pflRate);
	}
}