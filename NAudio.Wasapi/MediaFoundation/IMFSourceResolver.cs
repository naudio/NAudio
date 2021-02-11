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
		int CreateObjectFromURL(string pwszURL, uint dwFlags, IPropertyStore pProps, out MF_OBJECT_TYPE pObjectType, [MarshalAs(UnmanagedType.Interface)]out object ppObject);
		/// <summary>
		/// Creates a media source from a byte stream. This method is synchronous.
		/// </summary>
		void CreateObjectFromByteStream(IMFByteStream pByteStream, string pwszURL, uint dwFlags,IPropertyStore pProps,out MF_OBJECT_TYPE pObjectType,[MarshalAs(UnmanagedType.Interface)]out object ppObject);
		/// <summary>
		/// Begins an asynchronous request to create a media source or a byte stream from a URL.
		/// </summary>
		void BeginCreateObjectFromURL(string pwszURL, SourceResolverFlags dwFlags,IPropertyStore pProps,out IntPtr ppIUnknownCancelCookie,IntPtr pCallback,IntPtr punkState);
		/// <summary>
		/// Completes an asynchronous request to create an object from a URL.
		/// </summary>
		void EndCreateObjectFromURL(IntPtr pResult,out MF_OBJECT_TYPE pObjectType,[MarshalAs(UnmanagedType.IUnknown)]out object ppObject);
		/// <summary>
		/// Begins an asynchronous request to create a media source from a byte stream.
		/// </summary>
		void BeginCreateObjectFromByteStream(IMFByteStream pByteStream,string pwszURL, SourceResolverFlags dwFlags,IPropertyStore pProps,out IntPtr ppIUnknownCancelCookie,IntPtr pCallback,IntPtr punkState);
		/// <summary>
		/// Completes an asynchronous request to create a media source from a byte stream.
		/// </summary>
		void EndCreateObjectFromByteStream(IntPtr pResult,out MF_OBJECT_TYPE pObjectType, [MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
		/// <summary>
		/// Cancels an asynchronous request to create an object.
		/// </summary>
		void CancelObjectCreation(IntPtr pIUnknownCancelCookie);
	}
	public enum SourceResolverFlags
    {
		MF_RESOLUTION_MEDIASOURCE = 0x00000001,
		MF_RESOLUTION_BYTESTREAM = 0x00000002,
		MF_RESOLUTION_CONTENT_DOES_NOT_HAVE_TO_MATCH_EXTENSION_OR_MIME_TYPE = 0x00000010,
		MF_RESOLUTION_KEEP_BYTE_STREAM_ALIVE_ON_FAIL = 0x00000020,
		MF_RESOLUTION_READ = 0x00010000,
		MF_RESOLUTION_WRITE = 0x00020000,
		MF_RESOLUTION_DISABLE_LOCAL_PLUGINS = 0x00000040
    }
}