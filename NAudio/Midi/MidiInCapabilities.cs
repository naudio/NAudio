using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Midi
{
    /// <summary>
    /// MIDI In Device Capabilities
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MidiInCapabilities
    {
        /// <summary>
        /// wMid
        /// </summary>
        UInt16 manufacturerId;
        /// <summary>
        /// wPid
        /// </summary>
        UInt16 productId;
        /// <summary>
        /// vDriverVersion
        /// </summary>
        UInt32 driverVersion;
        /// <summary>
        /// Product Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        string productName;
        /// <summary>
        /// Support - Reserved
        /// </summary>
        Int32 support;

        private const int MaxProductNameLength = 32;

		/// <summary>
		/// Gets the manufacturer of this device
		/// </summary>
		public Manufacturers Manufacturer
		{
			get 
			{
                return (Manufacturers)manufacturerId;
			}
		}
		
		/// <summary>
		/// Gets the product identifier (manufacturer specific)
		/// </summary>
		public int ProductId 
		{
			get 
			{
                return productId;
			}
		}
		
		/// <summary>
		/// Gets the product name
		/// </summary>
		public string ProductName 
		{
			get 
			{
                return productName;
			}
		}
    }
}
