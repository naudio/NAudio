using System;

namespace NAudio.Wave.Alsa
{
    /// <summary>
    /// Exception thrown when an ALSA (<c>libasound</c>) call returns an error.
    /// </summary>
    public class AlsaException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="AlsaException"/> from an ALSA error code.
        /// </summary>
        /// <param name="errorCode">The negative error code returned by libasound.</param>
        public AlsaException(int errorCode)
            : base(AlsaInterop.ErrorString(errorCode))
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Creates a new <see cref="AlsaException"/> from an ALSA error code,
        /// prefixed with the name of the failing call.
        /// </summary>
        /// <param name="function">The libasound function that failed.</param>
        /// <param name="errorCode">The negative error code returned by libasound.</param>
        public AlsaException(string function, int errorCode)
            : base($"{function}: {AlsaInterop.ErrorString(errorCode)}")
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// The raw ALSA error code (a negative <c>errno</c>-style value).
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Throws an <see cref="AlsaException"/> if <paramref name="errorCode"/>
        /// indicates failure (a negative value).
        /// </summary>
        /// <param name="errorCode">The result of a libasound call.</param>
        /// <param name="function">The libasound function that produced it.</param>
        public static void ThrowIfError(int errorCode, string function)
        {
            if (errorCode < 0)
            {
                throw new AlsaException(function, errorCode);
            }
        }
    }
}
