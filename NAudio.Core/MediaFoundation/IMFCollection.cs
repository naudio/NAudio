using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Represents a generic collection of IUnknown pointers. 
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("5BC8A76B-869A-46A3-9B03-FA218A66AEBE")]
    public interface IMFCollection
    {
        /// <summary>
        /// Retrieves the number of objects in the collection.
        /// </summary>
        void GetElementCount(out int pcElements);

        /// <summary>
        /// Retrieves an object in the collection.
        /// </summary>
        void GetElement([In] int dwElementIndex, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnkElement);

        /// <summary>
        /// Adds an object to the collection.
        /// </summary>
        void AddElement([In, MarshalAs(UnmanagedType.IUnknown)] object pUnkElement);

        /// <summary>
        /// Removes an object from the collection.
        /// </summary>
        void RemoveElement([In] int dwElementIndex, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppUnkElement);
        
        /// <summary>
        /// Removes an object from the collection.
        /// </summary>
        void InsertElementAt([In] int dwIndex, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        void RemoveAllElements();
    }
}
