using System;
using System.Text;
using NAudio.Wave;

namespace NAudio
{
    /// <summary>
    /// Summary description for MmException.
    /// </summary>
    public class MmException : Exception
    {
        /// <summary>
        /// Creates a new MmException
        /// </summary>
        /// <param name="result">The result returned by the Windows API call</param>
        /// <param name="function">The name of the Windows API that failed</param>
        public MmException(MmResult result, string function)
            : base(ErrorMessage(result, function))
        {
            Result = result;
            Function = function;
        }


        // MAXERRORLENGTH from mmsystem.h
        private const int ErrorTextMaxLength = 256;

        private static string GetErrorText(MmResult result)
        {
            var buffer = new StringBuilder(ErrorTextMaxLength);
            if (WaveInterop.waveOutGetErrorText(result, buffer, (uint)buffer.Capacity) == MmResult.NoError)
            {
                return buffer.ToString();
            }

            return string.Empty;
        }

        private static string ErrorMessage(MmResult result, string function)
        {
            var errorText = GetErrorText(result);
            return errorText.Length > 0
                ? $"{result} calling {function}: {errorText}"
                : $"{result} calling {function}";
        }

        /// <summary>
        /// Helper function to automatically raise an exception on failure
        /// </summary>
        /// <param name="result">The result of the API call</param>
        /// <param name="function">The API function name</param>
        public static void Try(MmResult result, string function)
        {
            if (result != MmResult.NoError)
                throw new MmException(result, function);
        }

        /// <summary>
        /// Returns the Windows API result
        /// </summary>
        public MmResult Result { get; }

        /// <summary>
        /// The function being called
        /// </summary>
        public string Function { get; }

    }
}
