using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Exception thrown by Media Foundation API operations.
    /// Inherits from COMException for backwards compatibility with code that catches COMException.
    /// </summary>
    public class MediaFoundationException : COMException
    {
        /// <summary>
        /// Creates a new MediaFoundationException
        /// </summary>
        public MediaFoundationException(int hresult)
            : base(GetMessageForHResult(hresult), hresult)
        {
        }

        /// <summary>
        /// Creates a new MediaFoundationException with a message
        /// </summary>
        public MediaFoundationException(string message, int hresult)
            : base(message, hresult)
        {
        }

        /// <summary>
        /// Throws a MediaFoundationException if the HRESULT indicates failure.
        /// </summary>
        public static void ThrowIfFailed(int hresult)
        {
            if (hresult < 0)
                throw new MediaFoundationException(hresult);
        }

        private static string GetMessageForHResult(int hr) => hr switch
        {
            // General Media Foundation errors
            MediaFoundationErrors.MF_E_PLATFORM_NOT_INITIALIZED => "Media Foundation platform not initialized. Call MFStartup() first.",
            MediaFoundationErrors.MF_E_BUFFERTOOSMALL => "The buffer was too small to carry out the requested action.",
            MediaFoundationErrors.MF_E_INVALIDREQUEST => "The request is invalid in the current state.",
            MediaFoundationErrors.MF_E_INVALIDSTREAMNUMBER => "The stream number provided was invalid.",
            MediaFoundationErrors.MF_E_INVALIDMEDIATYPE => "The media type is invalid, inconsistent, or not supported by this object.",
            MediaFoundationErrors.MF_E_NOTACCEPTING => "The callee is currently not accepting further input.",
            MediaFoundationErrors.MF_E_NOT_INITIALIZED => "This object needs to be initialized before the requested operation can be carried out.",
            MediaFoundationErrors.MF_E_UNSUPPORTED_REPRESENTATION => "The requested representation is not supported.",
            MediaFoundationErrors.MF_E_NO_MORE_TYPES => "No more media types are available.",
            MediaFoundationErrors.MF_E_UNSUPPORTED_SERVICE => "The requested service is not supported.",
            MediaFoundationErrors.MF_E_UNEXPECTED => "An unexpected error occurred in the Media Foundation pipeline.",
            MediaFoundationErrors.MF_E_INVALIDNAME => "The name is invalid.",
            MediaFoundationErrors.MF_E_INVALIDTYPE => "The type is invalid.",
            MediaFoundationErrors.MF_E_INVALID_FILE_FORMAT => "The file format is invalid or not recognized.",
            MediaFoundationErrors.MF_E_INVALIDINDEX => "The index is invalid.",
            MediaFoundationErrors.MF_E_INVALID_TIMESTAMP => "The timestamp is invalid.",
            MediaFoundationErrors.MF_E_UNSUPPORTED_SCHEME => "The URL scheme is not supported.",
            MediaFoundationErrors.MF_E_UNSUPPORTED_BYTESTREAM_TYPE => "The byte stream type is not supported.",
            MediaFoundationErrors.MF_E_UNSUPPORTED_TIME_FORMAT => "The time format is not supported.",
            MediaFoundationErrors.MF_E_NO_SAMPLE_TIMESTAMP => "No sample timestamp was provided.",
            MediaFoundationErrors.MF_E_NO_SAMPLE_DURATION => "No sample duration was provided.",
            MediaFoundationErrors.MF_E_INVALID_STREAM_DATA => "The stream data is invalid.",
            MediaFoundationErrors.MF_E_UNSUPPORTED_RATE => "The playback rate is not supported.",
            MediaFoundationErrors.MF_E_NOT_FOUND => "The requested object or value was not found.",
            MediaFoundationErrors.MF_E_NOT_AVAILABLE => "The requested resource is not available.",
            MediaFoundationErrors.MF_E_SAMPLE_NOT_WRITABLE => "The sample is not writable.",
            MediaFoundationErrors.MF_E_BAD_STARTUP_VERSION => "The MFStartup version does not match.",
            MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND => "The requested attribute was not found.",
            MediaFoundationErrors.MF_E_PROPERTY_TYPE_NOT_SUPPORTED => "The property type is not supported.",
            MediaFoundationErrors.MF_E_UNRECOVERABLE_ERROR_OCCURRED => "An unrecoverable error occurred.",

            // Transform errors
            MediaFoundationErrors.MF_E_TRANSFORM_TYPE_NOT_SET => "The transform media type has not been set.",
            MediaFoundationErrors.MF_E_TRANSFORM_STREAM_CHANGE => "The transform output stream has changed.",
            MediaFoundationErrors.MF_E_TRANSFORM_INPUT_REMAINING => "The transform has input remaining to process.",
            MediaFoundationErrors.MF_E_TRANSFORM_NEED_MORE_INPUT => "The transform needs more input data before it can produce output.",
            MediaFoundationErrors.MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING => "Cannot change media type while the transform is processing.",

            _ => $"Media Foundation error 0x{hr:X8}"
        };
    }
}
