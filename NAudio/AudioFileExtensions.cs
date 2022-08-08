using System;

namespace NAudio
{
    public class AudioFileExtensions
    {
        /// <summary>
        /// Converts a sound file extension to an enumeration value
        /// </summary>
        /// <param name="fileExt">The file extension to convert. Case is ignored. It must include the period ('.').</param>
        /// <returns>The enumeration value</returns>
        /// <remarks>Null file extensions will return as 'unknown'</remarks>
        public AudioFileFormatEnum GetFormatFromFileExt(string fileExt)
        {
            if (fileExt != null)
            {
                switch (fileExt.ToLower())
                {
                    case ".mp3":
                        return AudioFileFormatEnum.MP3;
                    case ".wav":
                        return AudioFileFormatEnum.WAV;
                    case ".aif":
                        return AudioFileFormatEnum.AIFF;
                    case ".aiff":
                        return AudioFileFormatEnum.AIFF;
                    default:
                        return AudioFileFormatEnum.Unknown;
                }
            }
            else
            {
                return AudioFileFormatEnum.Unknown;
            }
        }
    }
}
