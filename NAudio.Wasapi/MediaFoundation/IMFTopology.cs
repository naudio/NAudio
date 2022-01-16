using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
	/// <summary>
	/// IMFTopology interface
	/// </summary>
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true), ComImport, Guid("83CF873A-F6DA-4bc8-823F-BACFD55DC433")]
	public interface IMFTopology
	{
		/// <summary>
		/// Retrieves the value associated with a key.
		/// </summary>
		void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] IntPtr pValue);

		/// <summary>
		/// Retrieves the data type of the value associated with a key.
		/// </summary>
		void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int pType);

		/// <summary>
		/// Queries whether a stored attribute value equals a specified PROPVARIANT.
		/// </summary>
		void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, IntPtr value, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

		/// <summary>
		/// Compares the attributes on this object with the attributes on another object.
		/// </summary>
		void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, int matchType, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

		/// <summary>
		/// Retrieves a UINT32 value associated with a key.
		/// </summary>
		void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int punValue);

		/// <summary>
		/// Retrieves a UINT64 value associated with a key.
		/// </summary>
		void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out long punValue);

		/// <summary>
		/// Retrieves a double value associated with a key.
		/// </summary>
		void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);

		/// <summary>
		/// Retrieves a GUID value associated with a key.
		/// </summary>
		void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);

		/// <summary>
		/// Retrieves the length of a string value associated with a key.
		/// </summary>
		void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int pcchLength);

		/// <summary>
		/// Retrieves a wide-character string associated with a key.
		/// </summary>
		void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszValue, int cchBufSize,
					   out int pcchLength);

		/// <summary>
		/// Retrieves a wide-character string associated with a key. This method allocates the memory for the string.
		/// </summary>
		void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue,
								out int pcchLength);

		/// <summary>
		/// Retrieves the length of a byte array associated with a key.
		/// </summary>
		void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int pcbBlobSize);

		/// <summary>
		/// Retrieves a byte array associated with a key.
		/// </summary>
		void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, int cbBufSize,
					 out int pcbBlobSize);

		/// <summary>
		/// Retrieves a byte array associated with a key. This method allocates the memory for the array.
		/// </summary>
		void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out IntPtr ip, out int pcbSize);

		/// <summary>
		/// Retrieves an interface pointer associated with a key.
		/// </summary>
		void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
						[MarshalAs(UnmanagedType.IUnknown)] out object ppv);

		/// <summary>
		/// Associates an attribute value with a key.
		/// </summary>
		void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, IntPtr Value);

		/// <summary>
		/// Removes a key/value pair from the object's attribute list.
		/// </summary>
		void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);

		/// <summary>
		/// Removes all key/value pairs from the object's attribute list.
		/// </summary>
		void DeleteAllItems();

		/// <summary>
		/// Associates a UINT32 value with a key.
		/// </summary>
		void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, int unValue);

		/// <summary>
		/// Associates a UINT64 value with a key.
		/// </summary>
		void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, long unValue);

		/// <summary>
		/// Associates a double value with a key.
		/// </summary>
		void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);

		/// <summary>
		/// Associates a GUID value with a key.
		/// </summary>
		void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);

		/// <summary>
		/// Associates a wide-character string with a key.
		/// </summary>
		void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);

		/// <summary>
		/// Associates a byte array with a key.
		/// </summary>
		void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf,
					 int cbBufSize);

		/// <summary>
		/// Associates an IUnknown pointer with a key.
		/// </summary>
		void SetUnknown([MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);

		/// <summary>
		/// Locks the attribute store so that no other thread can access it.
		/// </summary>
		void LockStore();

		/// <summary>
		/// Unlocks the attribute store.
		/// </summary>
		void UnlockStore();

		/// <summary>
		/// Retrieves the number of attributes that are set on this object.
		/// </summary>
		void GetCount(out int pcItems);

		/// <summary>
		/// Retrieves an attribute at the specified index.
		/// </summary>
		void GetItemByIndex(int unIndex, out Guid pGuidKey, [In, Out] IntPtr pValue);

		/// <summary>
		/// Copies all of the attributes from this object into another attribute store.
		/// </summary>
		void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);
		/// <summary>
		/// Gets the identifier of the topology.
		/// </summary>
		void GetTopologyID(out ulong pID);
		/// <summary>
		/// Adds a node to the topology.
		/// </summary>
		void AddNode(IMFTopologyNode pNode);
		/// <summary>
		/// Removes a node from the topology.
		/// </summary>
		void RemoveNode(IMFTopologyNode pNode);
		/// <summary>
		/// Gets the number of nodes in the topology.
		/// </summary>
		void GetNodeCount(out ushort pwNodes);
		/// <summary>
		/// Gets a node in the topology, specified by index.
		/// </summary>
		void GetNode(ushort wIndex, out IMFTopologyNode ppNode);
		/// <summary>
		/// Removes all nodes from the topology.
		/// </summary>
		void Clear();
		/// <summary>
		/// Converts this topology into a copy of another topology.
		/// </summary>
		void CloneFrom(IMFTopology pTopology);
		/// <summary>
		/// Gets a node in the topology, specified by node identifier.
		/// </summary>
		void GetNodeByID(ulong qwTopoNodeID, out IMFTopologyNode ppNode);
		/// <summary>
		/// Gets the source nodes in the topology.
		/// </summary>
		void GetSourceNodeCollection(out IMFCollection ppCollection);
		/// <summary>
		/// Gets the output nodes in the topology.
		/// </summary>
		void GetOutputNodeCollection(out IMFCollection ppCollection);
	}
}