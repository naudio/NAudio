using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Represents an installed ACM Driver
    /// </summary>
    public class AcmDriver : IDisposable
    {
        private static List<AcmDriver> drivers;
        private AcmDriverDetails details;
        private IntPtr driverId;
        private IntPtr driverHandle;
        private List<AcmFormatTag> formatTags;
        private List<AcmFormat> tempFormatsList; // used by enumerator
        private IntPtr localDllHandle;

        /// <summary>
        /// Helper function to determine whether a particular codec is installed
        /// </summary>
        /// <param name="shortName">The short name of the function</param>
        /// <returns>Whether the codec is installed</returns>
        public static bool IsCodecInstalled(string shortName)
        {
            foreach (AcmDriver driver in EnumerateAcmDrivers())
            {
                if (driver.ShortName == shortName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to add a new ACM driver from a file
        /// </summary>
        /// <param name="driverFile">Full path of the .acm or dll file containing the driver</param>
        /// <returns>Handle to the driver</returns>
        public static AcmDriver AddLocalDriver(string driverFile)
        {
            IntPtr handle = NativeMethods.LoadLibrary(driverFile);
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Failed to load driver file");
            }
            var driverProc = NativeMethods.GetProcAddress(handle, "DriverProc");
            if (driverProc == IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(handle);
                throw new ArgumentException("Failed to discover DriverProc");
            }
            var result = AcmInterop.acmDriverAdd(out IntPtr driverHandle,
                handle, driverProc, 0, AcmDriverAddFlags.Function);
            if (result != MmResult.NoError)
            {
                NativeMethods.FreeLibrary(handle);
                throw new MmException(result, "acmDriverAdd");
            }
            var driver = new AcmDriver(driverHandle);
            // long name seems to be missing when we use acmDriverAdd
            if (string.IsNullOrEmpty(driver.details.longName))
            {
                driver.details.longName = "Local driver: " + Path.GetFileName(driverFile);
                driver.localDllHandle = handle;
            }
            return driver;
        }

        /// <summary>
        /// Removes a driver previously added using AddLocalDriver
        /// </summary>
        /// <param name="localDriver">Local driver to remove</param>
        public static void RemoveLocalDriver(AcmDriver localDriver)
        {
            if (localDriver.localDllHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Please pass in the AcmDriver returned by the AddLocalDriver method");
            }
            var removeResult = AcmInterop.acmDriverRemove(localDriver.driverId, 0); // gets stored as a driver Id
            NativeMethods.FreeLibrary(localDriver.localDllHandle);
            MmException.Try(removeResult, "acmDriverRemove");
        }

        /// <summary>
        /// Show Format Choose Dialog
        /// </summary>
        /// <param name="ownerWindowHandle">Owner window handle, can be null</param>
        /// <param name="windowTitle">Window title</param>
        /// <param name="enumFlags">Enumeration flags. None to get everything</param>
        /// <param name="enumFormat">Enumeration format. Only needed with certain enumeration flags</param>
        /// <param name="selectedFormat">The selected format</param>
        /// <param name="selectedFormatDescription">Textual description of the selected format</param>
        /// <param name="selectedFormatTagDescription">Textual description of the selected format tag</param>
        /// <returns>True if a format was selected</returns>
        public static bool ShowFormatChooseDialog(
            IntPtr ownerWindowHandle,
            string windowTitle,
            AcmFormatEnumFlags enumFlags,
            WaveFormat enumFormat,
            out WaveFormat selectedFormat,
            out string selectedFormatDescription,
            out string selectedFormatTagDescription)
        {
            AcmFormatChoose formatChoose = new AcmFormatChoose();
            formatChoose.structureSize = Marshal.SizeOf(formatChoose);
            formatChoose.styleFlags = AcmFormatChooseStyleFlags.None;
            formatChoose.ownerWindowHandle = ownerWindowHandle;
            int maxFormatSize = 200; // guess
            formatChoose.selectedWaveFormatPointer = Marshal.AllocHGlobal(maxFormatSize);
            formatChoose.selectedWaveFormatByteSize = maxFormatSize;
            formatChoose.title = windowTitle;
            formatChoose.name = null;
            formatChoose.formatEnumFlags = enumFlags;//AcmFormatEnumFlags.None;
            formatChoose.waveFormatEnumPointer = IntPtr.Zero;
            if (enumFormat != null)
            {
                IntPtr enumPointer = Marshal.AllocHGlobal(Marshal.SizeOf(enumFormat));
                Marshal.StructureToPtr(enumFormat,enumPointer,false);
                formatChoose.waveFormatEnumPointer = enumPointer;
            }
            formatChoose.instanceHandle = IntPtr.Zero;
            formatChoose.templateName = null;

            MmResult result = AcmInterop.acmFormatChoose(ref formatChoose);
            selectedFormat = null;
            selectedFormatDescription = null;
            selectedFormatTagDescription = null;
            if (result == MmResult.NoError)
            {
                selectedFormat = WaveFormat.MarshalFromPtr(formatChoose.selectedWaveFormatPointer);
                selectedFormatDescription = formatChoose.formatDescription;
                selectedFormatTagDescription = formatChoose.formatTagDescription;
            }            
            
            Marshal.FreeHGlobal(formatChoose.waveFormatEnumPointer);
            Marshal.FreeHGlobal(formatChoose.selectedWaveFormatPointer);
            if(result != MmResult.AcmCancelled && result != MmResult.NoError)
            {                
                throw new MmException(result, "acmFormatChoose");
            }
            return result == MmResult.NoError;
            
        }

        /// <summary>
        /// Gets the maximum size needed to store a WaveFormat for ACM interop functions
        /// </summary>
        public int MaxFormatSize
        {
            get
            {
                MmException.Try(AcmInterop.acmMetrics(driverHandle, AcmMetrics.MaxSizeFormat, out int maxFormatSize), "acmMetrics");
                return maxFormatSize;
            }
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
            MmException.Try(AcmInterop.acmDriverEnum(new AcmInterop.AcmDriverEnumCallback(DriverEnumCallback), IntPtr.Zero, 0), "acmDriverEnum");
            return drivers;
        }

        /// <summary>
        /// The callback for acmDriverEnum
        /// </summary>
        private static bool DriverEnumCallback(IntPtr hAcmDriver, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            drivers.Add(new AcmDriver(hAcmDriver));
            return true;
        }

        /// <summary>
        /// Creates a new ACM Driver object
        /// </summary>
        /// <param name="hAcmDriver">Driver handle</param>
        private AcmDriver(IntPtr hAcmDriver)
        {
            driverId = hAcmDriver;
            details = new AcmDriverDetails();
            details.structureSize = Marshal.SizeOf(details);
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
        public IntPtr DriverId
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
            var formatDetails = new AcmFormatDetails();
            formatDetails.structSize = Marshal.SizeOf(formatDetails);
            // need to make sure we have enough space for a waveFormat. formatTag.FormatSize isn't reliable, 
            // and some codecs MaxFormatSize isn't either
            formatDetails.waveFormatByteSize = 1024;
            formatDetails.waveFormatPointer = Marshal.AllocHGlobal(formatDetails.waveFormatByteSize);
            formatDetails.formatTag = (int)formatTag.FormatTag; // (int)WaveFormatEncoding.Unknown
            var result = AcmInterop.acmFormatEnum(driverHandle, 
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

        private bool AcmFormatTagEnumCallback(IntPtr hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            formatTags.Add(new AcmFormatTag(formatTagDetails));
            return true;
        }

        private bool AcmFormatEnumCallback(IntPtr hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags)
        {
            tempFormatsList.Add(new AcmFormat(formatDetails));
            return true;
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
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
