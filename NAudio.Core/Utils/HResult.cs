using System;
using System.Runtime.InteropServices;

namespace NAudio.Utils
{
    /// <summary>
    /// HResult - Helpers around the Windows HRESULT error type.
    /// </summary>
    public static partial class HResult
    {
        /// <inheritdoc cref="CommonHResults.S_OK"/>
        // [Obsolete("Should use CommonHResults.S_OK", false)]
        public const int S_OK = CommonHResults.S_OK;
        
        /// <inheritdoc cref="CommonHResults.S_FALSE"/>
        // [Obsolete("Should use CommonHResults.S_FALSE", false)]
        public const int S_FALSE = CommonHResults.S_FALSE;

        /// <inheritdoc cref="CommonHResults.E_INVALIDARG"/>
        // [Obsolete("Should use CommonHResults.E_INVALIDARG", false)]
        public const int E_INVALIDARG = CommonHResults.E_INVALIDARG;

        /// <summary>
        /// MAKE_HRESULT macro
        /// </summary>
        public static int MAKE_HRESULT(int sev, int fac, int code) => (int)(((uint)sev) << 31 | ((uint)fac) << 16 | ((uint)code));

        /// <summary>
        /// Gets a value whether the specified HRESULT represents an error.
        /// </summary>
        /// <param name="h_result">The HRESULT code to test.</param>
        /// <returns>A value whether <paramref name="h_result"/> is an error code or not.</returns>
        public static bool IsError(int h_result) => (h_result >> 31) == 1;

        /// <summary>
        /// Extracts the <see cref="Facility"/> code from the specified HRESULT code.
        /// </summary>
        /// <remarks>This corresponds to the HRESULT_FACILITY macro.</remarks>
        /// <param name="h_result">The HRESULT code to extract the <see cref="Facility"/> code from.</param>
        /// <returns>The <see cref="Facility"/> code extracted from the <paramref name="h_result"/> value.</returns>
        public static Facility ExtractFacility(int h_result) => (Facility)((h_result >> 16) & 0x1FFFU);

        /// <summary>
        /// Extracts the <see cref="Severity"/> code from the specified HRESULT code.
        /// </summary>
        /// <remarks>This corresponds to the HRESULT_SEVERITY macro.</remarks>
        /// <param name="h_result">The HRESULT code to extract the <see cref="Severity"/> code from.</param>
        /// <returns>The <see cref="Severity"/> code extracted from the <paramref name="h_result"/> value.</returns>
        public static Severity ExtractSeverity(int h_result) => (Severity)((h_result >> 31) & 0x1U);

        /// <summary>
        /// Helper to deal with the fact that in Win Store apps,
        /// the HResult property name has changed
        /// </summary>
        /// <param name="exception">COM Exception</param>
        /// <returns>The HResult</returns>
        public static int GetHResult(this COMException exception) => exception.ErrorCode;
    }
}
