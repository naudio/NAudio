using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.Utils
{
    /// <summary>
    /// Provides helpers around the Windows <c>HRESULT</c> type, when doing COM interoperation.
    /// </summary>
    public static class HResult
    {
        /// <summary>Success.</summary>
        public const int S_OK = 0x00000000;

        /// <summary>The call succeeded, but returned <see langword="false"/>.</summary>
        public const int S_FALSE = 0x00000001;

        /// <summary>Catastrophic failure</summary>
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

        /// <summary>Not implemented</summary>
        public const int E_NOTIMPL = unchecked((int)0x80004001);

        /// <summary>Ran out of memory</summary>
        public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);

        /// <summary>One or more arguments are invalid</summary>
        public const int E_INVALIDARG = unchecked((int)0x80070057);

        /// <summary>No such interface supported</summary>
        public const int E_NOINTERFACE = unchecked((int)0x80004002);

        /// <summary>Invalid pointer</summary>
        public const int E_POINTER = unchecked((int)0x80004003);

        /// <summary>Invalid handle</summary>
        public const int E_HANDLE = unchecked((int)0x80070006);

        /// <summary>Operation aborted</summary>
        public const int E_ABORT = unchecked((int)0x80004004);

        /// <summary>Unspecified error</summary>
        public const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>General access denied error</summary>
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);

        /// <summary>The data necessary to complete this operation is not yet available.</summary>
        public const int E_PENDING = unchecked((int)0x8000000A);

        /// <summary>The operation attempted to access data outside the valid range</summary>
        public const int E_BOUNDS = unchecked((int)0x8000000B);

        /// <summary>A concurrent or interleaved operation changed the state of the object, invalidating this operation.</summary>
        public const int E_CHANGEDSTATE = unchecked((int)0x8000000C);

        /// <summary>An illegal state change was requested.</summary>
        public const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000D);

        /// <summary>A method was called at an unexpected time.</summary>
        public const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);

        /// <summary>String not null terminated.</summary>
        public const int E_STRING_NOT_NULL_TERMINATED = unchecked((int)0x80000017);

        /// <summary>A delegate was assigned when not allowed.</summary>
        public const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);

        /// <summary>The application is exiting and cannot service this request</summary>
        public const int E_APPLICATION_EXITING = unchecked((int)0x8000001A);

        /// <summary>The application view is exiting and cannot service this request</summary>
        public const int E_APPLICATION_VIEW_EXITING = unchecked((int)0x8000001B);

        /// <summary>The object must support the <c>IAgileObject</c> interface</summary>
        [SupportedOSPlatform("windows6.2")]
        public const int RO_E_MUST_BE_AGILE = unchecked((int)0x8000001C);

        /// <summary>Activating a single-threaded class from <see cref="System.Threading.ApartmentState.MTA"/> is not supported</summary>
        public const int RO_E_UNSUPPORTED_FROM_MTA = unchecked((int)0x8000001D);

        /// <summary>Unable to perform requested operation.</summary>
        public const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);

        /// <summary>%1 could not be found.</summary>
        public const int STG_E_FILENOTFOUND = unchecked((int)0x80030002);

        /// <summary>The path %1 could not be found.</summary>
        public const int STG_E_PATHNOTFOUND = unchecked((int)0x80030003);

        /// <summary>There are insufficient resources to open another file.</summary>
        public const int STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004);

        /// <summary>Access Denied.</summary>
        public const int STG_E_ACCESSDENIED = unchecked((int)0x80030005);

        /// <summary>Attempted an operation on an invalid object.</summary>
        public const int STG_E_INVALIDHANDLE = unchecked((int)0x80030006);

        /// <summary>There is insufficient memory available to complete operation.</summary>
        public const int STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008);

        /// <summary>Invalid pointer error.</summary>
        public const int STG_E_INVALIDPOINTER = unchecked((int)0x80030009);

        /// <summary>There are no more entries to return.</summary>
        public const int STG_E_NOMOREFILES = unchecked((int)0x80030012);

        /// <summary>Disk is write-protected.</summary>
        public const int STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013);

        /// <summary>An error occurred during a seek operation.</summary>
        public const int STG_E_SEEKERROR = unchecked((int)0x80030019);

        /// <summary>A disk error occurred during a write operation.</summary>
        public const int STG_E_WRITEFAULT = unchecked((int)0x8003001D);

        /// <summary>A disk error occurred during a read operation.</summary>
        public const int STG_E_READFAULT = unchecked((int)0x8003001E);

        /// <summary>A share violation has occurred.</summary>
        public const int STG_E_SHAREVIOLATION = unchecked((int)0x80030020);

        /// <summary>Invalid parameter error.</summary>
        public const int STG_E_INVALIDPARAMETER = unchecked((int)0x80030057);

        /// <summary>There is insufficient disk space to complete operation.</summary>
        public const int STG_E_MEDIUMFULL = unchecked((int)0x80030070);

        /// <summary>An unexpected error occurred.</summary>
        public const int STG_E_UNKNOWN = unchecked((int)0x800300FD);

        /// <summary>That function is not implemented.</summary>
        public const int STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE);

        /// <summary>Invalid flag error.</summary>
        public const int STG_E_INVALIDFLAG = unchecked((int)0x800300FF);

        /// <summary>Out of present range.</summary>
        public const int DISP_E_OVERFLOW = unchecked((int)0x8002000A);

        /// <summary>Type mismatch.</summary>
        public const int DISP_E_TYPEMISMATCH = unchecked((int)0x80020005);

        /// <summary>Bad variable type.</summary>
        public const int DISP_E_BADVARTYPE = unchecked((int)0x80020008);

        /// <summary>
        /// MAKE_HRESULT macro
        /// </summary>
        [Obsolete("Use MakeHResult instead.", false)]
        public static int MAKE_HRESULT(int sev, int fac, int code) => MakeHResult(sev, fac, code);

        /// <summary>
        /// Constructs a new <c>HRESULT</c> value from the specified severity, facility and error codes.
        /// </summary>
        /// <param name="severity">The severity that the returned <c>HRESULT</c> value will have.</param>
        /// <param name="facility">The facility that the returned <c>HRESULT</c> value will have.</param>
        /// <param name="code">The actual error code that the returned <c>HRESULT</c> value will have.</param>
        /// <returns>A newly constructed <c>HRESULT</c> value.</returns>
        public static int MakeHResult(int severity, int facility, int code) => (int)(((uint)severity) << 31 | ((uint)facility) << 16 | ((uint)code));

        /// <summary>
        /// Provides a value whether the specified <c>HRESULT</c> is actually an error code. <br />
        /// This corresponds to the <c>IS_ERROR(x)</c> macro.
        /// </summary>
        /// <param name="h_result">The <c>HRESULT</c> code to test against.</param>
        /// <returns>A <see cref="bool"/> value whether the specified <paramref name="h_result"/> is an error or not.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsError(int h_result) => h_result < 0;

        /// <summary>
        /// Helper to deal with the fact that in Win Store apps,
        /// the HResult property name has changed
        /// </summary>
        /// <param name="exception">COM Exception</param>
        /// <returns>The HResult</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHResult(this COMException exception) => exception.ErrorCode;
    }

}
