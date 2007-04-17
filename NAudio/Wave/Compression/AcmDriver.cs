using System;
using System.Collections.Generic;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents an installed ACM Driver
    /// </summary>
    public class AcmDriver
    {
        private static List<AcmDriver> drivers;
        private AcmDriverDetails details;
        private int driverId;

        /// <summary>
        /// Helper function to determine whether
        /// </summary>
        /// <param name="shortName"></param>
        /// <returns></returns>
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
        /// Gets a list of the ACM Drivers installed
        /// </summary>
        public static AcmDriver[] EnumerateAcmDrivers()
        {
            drivers = new List<AcmDriver>();
            AcmInterop.acmDriverEnum(new AcmInterop.AcmDriverEnumCallback(DriverEnumCallback), 0, 0);
            return drivers.ToArray();
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
            details.cbStruct = System.Runtime.InteropServices.Marshal.SizeOf(details);
            MmException.Try(AcmInterop.acmDriverDetails(hAcmDriver, ref details, 0), "acmDriverDetails");
        }

        /// <summary>
        /// The short name of this driver
        /// </summary>
        public string ShortName
        {
            get
            {
                return details.szShortName;
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

    }

}
