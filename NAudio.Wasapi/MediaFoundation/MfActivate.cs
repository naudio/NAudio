using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFActivate providing managed access to activation objects.
    /// </summary>
    public class MfActivate : IDisposable
    {
        private Interfaces.IMFActivate activateInterface;
        private IntPtr nativePointer;

        internal MfActivate(Interfaces.IMFActivate activateInterface, IntPtr nativePointer)
        {
            this.activateInterface = activateInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Gets the native COM pointer for this activate object (for passing to other COM methods).
        /// </summary>
        internal IntPtr NativePointer => nativePointer;

        /// <summary>
        /// Creates the object associated with this activation object.
        /// </summary>
        /// <param name="interfaceId">The interface ID to request.</param>
        /// <returns>The activated object as an IntPtr. The caller owns the reference.</returns>
        public IntPtr ActivateObject(Guid interfaceId)
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.ActivateObject(interfaceId, out var ppv));
            return ppv;
        }

        /// <summary>
        /// Activates the object as an IMFTransform and returns an MfTransform wrapper.
        /// </summary>
        /// <returns>The activated transform.</returns>
        public MfTransform ActivateTransform()
        {
            var iid = new Guid("bf94c121-5b05-4e6f-8000-ba598961414d"); // IID_IMFTransform
            MediaFoundationException.ThrowIfFailed(activateInterface.ActivateObject(iid, out var ppv));
            var transformInterface = (Interfaces.IMFTransform)Marshal.GetObjectForIUnknown(ppv);
            return new MfTransform(transformInterface, ppv);
        }

        /// <summary>
        /// Shuts down the created object.
        /// </summary>
        public void ShutdownObject()
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.ShutdownObject());
        }

        /// <summary>
        /// Detaches the created object from the activation object.
        /// </summary>
        public void DetachObject()
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.DetachObject());
        }

        /// <summary>
        /// Gets a UINT32 attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <returns>The attribute value.</returns>
        public int GetUInt32(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.GetUINT32(key, out var value));
            return value;
        }

        /// <summary>
        /// Gets a GUID attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <returns>The attribute value.</returns>
        public Guid GetGuid(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.GetGUID(key, out var value));
            return value;
        }

        /// <summary>
        /// Gets a string attribute.
        /// </summary>
        /// <param name="key">The attribute key GUID.</param>
        /// <returns>The string value.</returns>
        public string GetString(Guid key)
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.GetAllocatedString(key, out var ppwszValue, out var length));
            try
            {
                return Marshal.PtrToStringUni(ppwszValue, length);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ppwszValue);
            }
        }

        /// <summary>
        /// Gets the number of attributes set on this object.
        /// </summary>
        public int AttributeCount
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(activateInterface.GetCount(out var count));
                return count;
            }
        }

        /// <summary>
        /// Retrieves an attribute at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index of the attribute</param>
        /// <param name="key">Receives the attribute GUID key</param>
        /// <param name="valuePtr">Receives the attribute value as a PropVariant. Caller must free with PropVariant.Clear.</param>
        public void GetAttributeByIndex(int index, out Guid key, IntPtr valuePtr)
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.GetItemByIndex(index, out key, valuePtr));
        }

        /// <summary>
        /// Gets a blob attribute as an array of structs.
        /// </summary>
        public T[] GetBlobAsArrayOf<T>(Guid key) where T : new()
        {
            MediaFoundationException.ThrowIfFailed(activateInterface.GetAllocatedBlob(key, out var ppBuf, out var cbSize));
            try
            {
                int structSize = Marshal.SizeOf<T>();
                int count = cbSize / structSize;
                var result = new T[count];
                var ptr = ppBuf;
                for (int i = 0; i < count; i++)
                {
                    result[i] = Marshal.PtrToStructure<T>(ptr);
                    ptr += structSize;
                }
                return result;
            }
            finally
            {
                Marshal.FreeCoTaskMem(ppBuf);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            activateInterface = null;
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
