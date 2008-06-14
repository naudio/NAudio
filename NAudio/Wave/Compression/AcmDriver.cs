using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents an installed ACM Driver
    /// </summary>
    public class AcmDriver : IDisposable
    {
        private static List<AcmDriver> drivers;
        private AcmDriverDetails details;
        private int driverId;
        private IntPtr driverHandle;
        private List<AcmFormatTag> formatTags;
        private List<AcmFormat> tempFormatsList; // used by enumerator

        /// <summary>
        /// Helper function to determine whether a particular codec is installed
        /// </summary>
        /// <param name="shortName">The short name of the function</param>
        /// <returns>Whether the codec is installed</returns>
        public static bool IsCodecInstalled(string shortName)
        {
            foreach (AcmDriver driver in AcmDriver.EnumerateAcmDrivers())
            {
                if (driver.ShortName == shortName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Finds a Driver by its short name
        /// </summary>
        /// <param name="shortName">Short Name</param>
        /// <returns>The driver, or null if not found</returns>
        public static AcmDriver FindByShortName(string shortName)
        {
            foreach (AcmDriver driver in AcmDriver.EnumerateAcmDrivers())
            {
                if (driver.ShortName == shortName)
                {
                    return driver;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a list of the ACM Drivers installed
        /// </summary>
        public static IEnumerable<AcmDriver> EnumerateAcmDrivers()
        {
            drivers = new List<AcmDriver>();
            MmException.Try(AcmInterop.acmDriverEnum(new AcmInterop.AcmDriverEnumCallback(DriverEnumCallback), 0, 0), "acmDriverEnum");
            return drivers;
        }

        /// <summary>
        /// The callback for acmDriverEnum
        /// </summary>
        private static bool DriverEnumCallback(int hAcmDriver, int dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            drivers.Add(new AcmDriver(hAcmDriver));
            return true;
        }

        /// <summary>
        /// Creates a new ACM Driver object
        /// </summary>
        /// <param name="hAcmDriver">Driver handle</param>
        private AcmDriver(int hAcmDriver)
        {
            driverId = hAcmDriver;
            details = new AcmDriverDetails();
            details.structureSize = System.Runtime.InteropServices.Marshal.SizeOf(details);
            MmException.Try(AcmInterop.acmDriverDetails(hAcmDriver, ref details, 0), "acmDriverDetails");
        }

        /// <summary>
        /// The short name of this driver
        /// </summary>
        public string ShortName
        {
            get
            {
                return details.shortName;
            }
        }

        /// <summary>
        /// The full name of this driver
        /// </summary>
        public string LongName
        {
            get
            {
                return details.longName;
            }
        }

        /// <summary>
        /// The driver ID
        /// </summary>
        public int DriverId
        {
            get
            {
                return driverId;
            }
        }

        /// <summary>
        /// ToString
        /// </summary>        
        public override string ToString()
        {            
            return LongName;
        }

        /// <summary>
        /// The list of FormatTags for this ACM Driver
        /// </summary>
        public IEnumerable<AcmFormatTag> FormatTags
        {
            get
            {
                if (formatTags == null)
                {
                    if (driverHandle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Driver must be opened first");
                    }
                    formatTags = new List<AcmFormatTag>();
                    AcmFormatTagDetails formatTagDetails = new AcmFormatTagDetails();
                    formatTagDetails.structureSize = Marshal.SizeOf(formatTagDetails);
                    MmException.Try(AcmInterop.acmFormatTagEnum(this.driverHandle, ref formatTagDetails, AcmFormatTagEnumCallback, IntPtr.Zero, 0), "acmFormatTagEnum");
                }
                return formatTags;
            }
        }

        
        /// <summary>
        /// Gets all the supported formats for a given format tag
        /// </summary>
        /// <param name="formatTag">Format tag</param>
        /// <returns>Supported formats</returns>
        public IEnumerable<AcmFormat> GetFormats(AcmFormatTag formatTag)
        {
            if (driverHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Driver must be opened first");
            }
            tempFormatsList = new List<AcmFormat>();
            AcmFormatDetails formatDetails = new AcmFormatDetails();
            int maxFormatSize = 0;
            MmException.Try(AcmInterop.acmMetrics(driverHandle, AcmMetrics.MaxSizeFormat, out maxFormatSize),"acmMetrics");
            formatDetails.structSize = Marshal.SizeOf(formatDetails);
            formatDetails.waveFormatByteSize = maxFormatSize; // formatTag.FormatSize doesn't work;
            formatDetails.waveFormatPointer = Marshal.AllocHGlobal(formatDetails.waveFormatByteSize);
            formatDetails.formatTag = (int)formatTag.FormatTag; // (int)WaveFormatEncoding.Unknown
            MmResult result = AcmInterop.acmFormatEnum(driverHandle, 
                ref formatDetails, AcmFormatEnumCallback, IntPtr.Zero, 
                AcmFormatEnumFlags.None);
            Marshal.FreeHGlobal(formatDetails.waveFormatPointer);
            MmException.Try(result,"acmFormatEnum");
            return tempFormatsList;
        }

        /// <summary>
        /// Opens this driver
        /// </summary>
        public void Open()
        {
            if (driverHandle == IntPtr.Zero)
            {
                MmException.Try(AcmInterop.acmDriverOpen(out driverHandle, DriverId, 0), "acmDriverOpen");
            }
        }

        /// <summary>
        /// Closes this driver
        /// </summary>
        public void Close()
        {
            if(driverHandle != IntPtr.Zero)
            {
                MmException.Try(AcmInterop.acmDriverClose(driverHandle, 0),"acmDriverClose");
                driverHandle = IntPtr.Zero;
            }
        }

        private bool AcmFormatTagEnumCallback(int hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            formatTags.Add(new AcmFormatTag(formatTagDetails));
            return true;
        }

        private bool AcmFormatEnumCallback(int hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            tempFormatsList.Add(new AcmFormat(formatDetails));
            return true;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (driverHandle != IntPtr.Zero)
            {
                Close();
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }

}
