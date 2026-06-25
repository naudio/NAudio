using System;

namespace NAudio.SoundFile;

/// <summary>
/// Exception thrown when a <c>libsndfile</c> call fails.
/// </summary>
public class SoundFileException : Exception
{
    /// <summary>
    /// Creates a new <see cref="SoundFileException"/> with a message and
    /// the libsndfile error code that produced it.
    /// </summary>
    /// <param name="message">The libsndfile error string.</param>
    /// <param name="errorCode">The libsndfile error number, or 0 when not available.</param>
    public SoundFileException(string message, int errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a new <see cref="SoundFileException"/> wrapping an
    /// underlying failure (e.g. a backing-stream I/O exception that
    /// could not propagate through the native callback boundary).
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="errorCode">The libsndfile error number, or 0.</param>
    /// <param name="innerException">The original exception.</param>
    public SoundFileException(string message, int errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a new <see cref="SoundFileException"/> from a message and
    /// error code, prefixed with the name of the failing call.
    /// </summary>
    /// <param name="errorCode">The libsndfile error number, or 0 when not available.</param>
    /// <param name="function">The libsndfile function that failed.</param>
    /// <param name="message">The libsndfile error string.</param>
    public SoundFileException(int errorCode, string function, string message)
        : base($"{function}: {message}")
    {
        ErrorCode = errorCode;
        Function = function;
    }

    /// <summary>
    /// The raw libsndfile error number (<c>sf_error</c>), or 0 when not
    /// available (for example when <c>sf_open</c> returned NULL).
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// The libsndfile function that failed, or <c>null</c> when not supplied.
    /// </summary>
    public string Function { get; }

    /// <summary>
    /// Throws a <see cref="SoundFileException"/> describing the last error
    /// if <paramref name="sndfile"/> is in an error state.
    /// </summary>
    /// <param name="sndfile">An open <c>SNDFILE*</c> handle.</param>
    /// <param name="function">The libsndfile function just called.</param>
    internal static void ThrowIfError(IntPtr sndfile, string function)
    {
        int error = SndFileInterop.Error(sndfile);
        if (error != 0)
        {
            throw new SoundFileException(error, function, SndFileInterop.ErrorString(sndfile));
        }
    }

    /// <summary>
    /// Throws a <see cref="SoundFileException"/> when an open call
    /// returned NULL, using libsndfile's last-error string.
    /// </summary>
    /// <param name="sndfile">The handle returned by an <c>sf_open*</c> call.</param>
    /// <param name="function">The open function that was called.</param>
    internal static void ThrowIfOpenFailed(IntPtr sndfile, string function)
    {
        if (sndfile == IntPtr.Zero)
        {
            throw new SoundFileException(0, function, SndFileInterop.ErrorString(IntPtr.Zero));
        }
    }
}
