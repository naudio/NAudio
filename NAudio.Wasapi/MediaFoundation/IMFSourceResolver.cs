using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
	/// <summary>
	/// IMFSourceResolver interface
	/// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfsourceresolver
	/// </summary>
	[ComVisible(true), ComImport, Guid("FBE5A32D-A497-4b61-BB85-97B1A848A6E3")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMFSourceResolver
	{
		/// <summary>
		/// Creates a media source or a byte stream from a URL. This method is synchronous.
		/// </summary>
		void CreateObjectFromURL(string pwszURL, uint dwFlags, IPropertyStore pProps, out uint pObjectType, [MarshalAs(UnmanagedType.IUnknown)]out object ppObject);
		/// <summary>
		/// Creates a media source from a byte stream. This method is synchronous.
		/// </summary>
		void CreateObjectFromByteStream(IMFByteStream pByteStream, string pwszURL, uint dwFlags,IPropertyStore pProps,out MF_OBJECT_TYPE pObjectType,[MarshalAs(UnmanagedType.IUnknown)]out object ppObject);
		/// <summary>
		/// Begins an asynchronous request to create a media source or a byte stream from a URL.
		/// </summary>
		void BeginCreateObjectFromURL(string pwszURL, uint dwFlags,IPropertyStore pProps,out IntPtr ppIUnknownCancelCookie,IntPtr pCallback,IntPtr punkState);
		/// <summary>
		/// Completes an asynchronous request to create an object from a URL.
		/// </summary>
		void EndCreateObjectFromURL(IntPtr pResult,out MF_OBJECT_TYPE pObjectType,[MarshalAs(UnmanagedType.IUnknown)]out object ppObject);
		/// <summary>
		/// Begins an asynchronous request to create a media source from a byte stream.
		/// </summary>
		void BeginCreateObjectFromByteStream(IMFByteStream pByteStream,string pwszURL,uint dwFlags,IPropertyStore pProps,out IntPtr ppIUnknownCancelCookie,IntPtr pCallback,IntPtr punkState);
		/// <summary>
		/// Completes an asynchronous request to create a media source from a byte stream.
		/// </summary>
		void EndCreateObjectFromByteStream(IntPtr pResult,out MF_OBJECT_TYPE pObjectType, [MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
		/// <summary>
		/// Cancels an asynchronous request to create an object.
		/// </summary>
		void CancelObjectCreation(IntPtr pIUnknownCancelCookie);
	}
}