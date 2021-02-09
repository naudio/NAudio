using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFTopologyNode interface
    /// https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imftopologynode
    /// </summary>
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComVisible(true), ComImport, Guid("83CF873A-F6DA-4bc8-823F-BACFD55DC430")]
	public interface IMFTopologyNode
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
        /// Sets the object associated with this node.
        /// </summary>
        void SetObject([MarshalAs(UnmanagedType.IUnknown)]object pObject);
        /// <summary>
        /// Gets the object associated with this node.
        /// </summary>
		void GetObject([MarshalAs(UnmanagedType.IUnknown)]out object ppObject);
        /// <summary>
        /// Retrieves the node type.
        /// </summary>
		void GetNodeType(out uint pType);
        /// <summary>
        /// Retrieves the identifier of the node.
        /// </summary>
		void GetTopoNodeID(out ulong pID);
        /// <summary>
        /// Sets the identifier for the node.
        /// </summary>
		void SetTopoNodeID(ulong ullTopoID);
        /// <summary>
        /// Retrieves the number of input streams that currently exist on this node.
        /// </summary>
		void GetInputCount(out uint pcInputs);
        /// <summary>
        /// Retrieves the number of output streams that currently exist on this node.
        /// </summary>
		void GetOutputCount(out uint pcOutputs);
        /// <summary>
        /// Connects an output stream from this node to the input stream of another node.
        /// </summary>
		void ConnectOutput(uint dwOutputIndex, IMFTopologyNode pDownstreamNode, uint dwInputIndexOnDownstreamNode);
        /// <summary>
        /// Disconnects an output stream on this node.
        /// </summary>
		void DisconnectOutput(uint dwOutputIndex);
        /// <summary>
        /// Retrieves the node that is connected to a specified input stream on this node.
        /// </summary>
		void GetInput(uint dwInputIndex, out IMFTopologyNode ppUpstreamNode, out uint pdwOutputIndexOnUpstreamNode);
        /// <summary>
        /// Retrieves the node that is connected to a specified output stream on this node.
        /// </summary>
		void GetOutput(uint dwOutputIndex, out IMFTopologyNode ppDownstreamNode, out uint pdwInputIndexOnDownstreamNode);
        /// <summary>
        /// Sets the preferred media type for an output stream on this node.
        /// </summary>
		void SetOutputPrefType(uint dwOutputIndex, [MarshalAs(UnmanagedType.IUnknown)] object pType);
        /// <summary>
        /// Retrieves the preferred media type for an output stream on this node.
        /// </summary>
		void GetOutputPrefType(uint dwOutputIndex, [MarshalAs(UnmanagedType.IUnknown)]out object ppType);
        /// <summary>
        /// Sets the preferred media type for an input stream on this node.
        /// </summary>
		void SetInputPrefType(uint dwInputIndex, [MarshalAs(UnmanagedType.IUnknown)]object pType);
        /// <summary>
        /// Retrieves the preferred media type for an input stream on this node.
        /// </summary>
		void GetInputPrefType(uint dwInputIndex, [MarshalAs(UnmanagedType.IUnknown)]out object ppType);
        /// <summary>
        /// Copies the data from another topology node into this node.
        /// </summary>
		void CloneFrom(IMFTopologyNode pNode);
	}
}