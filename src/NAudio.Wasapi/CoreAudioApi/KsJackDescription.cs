using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// KS Jack Description
    /// </summary>
    public class KsJackDescription
    {
        private readonly IKsJackDescription ksJackDescriptionInterface;

        internal KsJackDescription(IKsJackDescription ksJackDescription)
        {
            ksJackDescriptionInterface = ksJackDescription;
        }

        /// <summary>
        /// Jack count
        /// </summary>
        public uint Count
        {
            get
            {
                ksJackDescriptionInterface.GetJackCount(out var result);
                return result;
            }
        }

        /// <summary>
        /// Get Jack Description by index
        /// </summary>
        public string this[uint index]
        {
            get
            {
                ksJackDescriptionInterface.GetJackDescription(index, out var result);
                return result;
            }
        }
    }
}
