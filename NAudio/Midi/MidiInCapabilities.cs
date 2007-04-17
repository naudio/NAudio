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
        public UInt16 manufacturerId;
        /// <summary>
        /// wPid
        /// </summary>
        public UInt16 productId;
        /// <summary>
        /// vDriverVersion
        /// </summary>
        public UInt32 driverVersion;
        /// <summary>
        /// Product Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        public string productName;
        /// <summary>
        /// Support - Reserved
        /// </summary>
        public Int32 support;

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
