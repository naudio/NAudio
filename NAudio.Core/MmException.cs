using System;
using System.Runtime.InteropServices;
using System.Text;

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

        private const int ErrorTextMaxLength = 256;

        // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-waveoutgeterrortext
        /// <summary>
        /// The waveOutGetErrorText function retrieves a textual description of the error identified by the given error number.
        /// </summary>
        /// <param name="mmrError">Error number.</param>
        /// <param name="pszText">Pointer to a buffer to be filled with the textual error description.</param>
        /// <param name="cchText">Size, in characters, of the buffer pointed to by pszText.</param>
        /// <returns>Returns MMSYSERR_NOERROR if successful or an error otherwise.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern MmResult waveOutGetErrorText(MmResult mmrError, StringBuilder pszText, uint cchText);

        private static string GetErrorText(MmResult result)
        {
            var sb = new StringBuilder(ErrorTextMaxLength);
            var textResult = waveOutGetErrorText(result, sb, ErrorTextMaxLength);
            if (textResult == MmResult.NoError)
            {
                return sb.ToString();
            }

            return textResult.ToString();
        }

        private static string ErrorMessage(MmResult result, string function)
        {
            return $"{result} calling {function}: {GetErrorText(result)}";
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
