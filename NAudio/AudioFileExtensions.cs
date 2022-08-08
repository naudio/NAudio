using System;
using static NAudio.Wave.AudioFileReader;

namespace NAudio
{
    public class AudioFileExtensions
    {
        /// <summary>
        /// Converts a sound file extensions to an enumeration value
        /// </summary>
        /// <param name="fileExt">The file extension to convert. Case is ignored. It must include the period ('.').</param>
        /// <returns>The enumeration value</returns>
        public SoundFormatEnum GetFormatFromFileExt(string fileExt)
        {
            if (fileExt != null)
            {
                switch (fileExt.ToLower())
                {
                    case ".mp3":
                        return SoundFormatEnum.MP3;
                    case ".wav":
                        return SoundFormatEnum.WAV;
                    case ".aif":
                        return SoundFormatEnum.AIFF;
                    case ".aiff":
                        return SoundFormatEnum.AIFF;
                    default:
                        return SoundFormatEnum.Unknown;
                }
            }
            else
            {
                return SoundFormatEnum.Unknown;
            }
        }
    }
}
