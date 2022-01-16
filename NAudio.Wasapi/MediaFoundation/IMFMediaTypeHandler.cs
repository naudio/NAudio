using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
	/// <summary>
	/// IMFMediaTypeHandler interface
	/// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfmediatypehandler
	/// </summary>
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true), ComImport, Guid("e93dcf6c-4b07-4e1e-8123-aa16ed6eadf5")]
	public interface IMFMediaTypeHandler
	{
		/// <summary>
		/// Queries whether the object supports a specified media type.
		/// </summary>
		int IsMediaTypeSupported(IMFMediaType pMediaType, out IMFMediaType ppMediaType);
		/// <summary>
		/// Retrieves the number of media types in the object's list of supported media types.
		/// </summary>
		int GetMediaTypeCount(out uint pdwTypeCount);
		/// <summary>
		/// Retrieves a media type from the object's list of supported media types.
		/// </summary>
		int GetMediaTypeByIndex(uint dwIndex, out IMFMediaType ppType);
		/// <summary>
		/// Sets the object's media type.
		/// </summary>
		int SetCurrentMediaType(IMFMediaType pMediaType);
		/// <summary>
		/// Retrieves the current media type of the object.
		/// </summary>
		int GetCurrentMediaType(out IMFMediaType ppMediaType);
		/// <summary>
		/// Gets the major media type of the object.
		/// </summary>
		int GetMajorType(out Guid pguidMajorType);
	}
}