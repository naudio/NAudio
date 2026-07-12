
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using NAudio.MacOS.CoreFoundationApi;

namespace NAudio.MacOS;

/// <summary>
/// Provides the base exception class for all the macOS 
/// native API errors. <br />
/// The errors typically provide an <c>OSStatus</c> error code describing 
/// the error condition, that's why this exception derives
/// from <see cref="ExternalException"/>. <br />
/// The macOS wrappers provided by this assembly 
/// will provide exception classes derived 
/// from this exception class.
/// </summary>
public partial class MacException : ExternalException
{
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [LibraryImport("/usr/lib/system/libsystem_c.dylib", EntryPoint = "strerror_r")]
    private static partial int ErrorStringFunction(int errorcode, ReadOnlySpan<byte> pString, nuint size);

    private unsafe static string RetrieveDescriptionIfPossible(int osStatus)
    {
        byte[] stringData = new byte[1024];

        byte tries = 5;
        int errnostatus;
        do
        {
            errnostatus = ErrorStringFunction(osStatus, stringData, new((uint)stringData.Length));
            if (errnostatus == 34) // ERANGE
            {
                stringData = new byte[stringData.Length + 1024];
            }
        } while (errnostatus != 0 && --tries > 0);
        if (tries == 0)
        {
            if (errnostatus == 16) // EBYSY
            {
                using var error = CFError.Create(new(osStatus), CFError.OSStatusDomain);
                string desc = error.Description;
                return string.IsNullOrWhiteSpace(desc) ? "An error was occurred." : desc;
            }
            else
            {
                return $"Unknown error retrieveing message {osStatus:x2}: {errnostatus:x2}";
            }
        }
        else
        {
            fixed (byte* d = stringData) { return new((sbyte*)d); }
        }
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="MacException"/> class with the specified 
    /// <c>OSStatus</c> error code.
    /// </summary>
    /// <param name="osStatus">The status code.</param>
    public MacException(int osStatus) : base(RetrieveDescriptionIfPossible(osStatus), osStatus) { }

    /// <summary>
    /// Constructs a new instance of the <see cref="MacException"/> class with the specified 
    /// <c>OSStatus</c> error code, as well as the custom error message to provide.
    /// </summary>
    /// <param name="message">The custom error message to provide. Can be <see langword="null"/>.</param>
    /// <param name="osStatus">The <c>OSStatus</c> error code to provide.</param>
    public MacException([AllowNull] string message, int osStatus) : base(message ?? RetrieveDescriptionIfPossible(osStatus), osStatus) { }

    /// <summary>
    /// Provides the <c>OSStatus</c> code that is the reason why 
    /// this <see cref="MacException"/> object was created for.
    /// </summary>
    // mdcdi1315: Only overriden purely for documentation reasons.
    public override int ErrorCode => base.ErrorCode;
}