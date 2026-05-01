namespace NAudio.Wave
{
    /// <summary>
    /// Defines arrays of type <see cref="ChannelType"/> to define common channel types that are used in PCM and other types of audio streams,
    /// and some helpers of these.
    /// </summary>
    public static class CommonChannelTypes
    {
        /// <summary>
        /// Gets the monophonic channel layout.
        /// </summary>
        public static ChannelType[] Mono => new[] { ChannelType.Left };

        /// <summary>
        /// Gets the stereophonic channel layout, which the first sample the left speaker data are first. <br />
        /// The second sample contains the right speaker data.
        /// </summary>
        public static ChannelType[] Stereo => new[] { ChannelType.Left, ChannelType.Right };

        /// <summary>
        /// Creates a default channel layout by specifying the number of channels only. <br />
        /// However, note that you shouldn't use this as a general rule of thumb since channels can go at any position and thus you should create this by hand. <br />
        /// This currently happens to be Windows-compatible.
        /// </summary>
        /// <param name="nch">The number of channels.</param>
        /// <returns>A default and typed layout.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">At least one channel must be specified in the <paramref name="nch"/> parameter.</exception>
        public static ChannelType[] CreateLayoutFromNumberOfChannels(int nch)
        {
            if (nch < 1) { throw new System.ArgumentOutOfRangeException(nameof(nch), "At least one channel must be specified."); }
            ChannelType[] channels = new ChannelType[nch];
            for (
                ChannelType ch = ChannelType.Left, chlast = (ChannelType)nch;
                ch < chlast; ch++
            ) {
                channels[(int)ch] = ch;
            }
            return channels;
        }

        /// <summary>
        /// Finds out whether the specified channel layout is the same one as the another specified layout. <br />
        /// Both arrays must have the same length. If not, it returns <see langword="false"/>.
        /// </summary>
        /// <param name="layout1">The layout to be tested on the second layout.</param>
        /// <param name="layout2">The layout to be looked up whether it is equal to the current layout.</param>
        /// <returns>A value indicating equality.</returns>
        public static bool IsEqualTo(this ChannelType[] layout1 , ChannelType[] layout2)
        {
            if (layout2 is null || layout1.LongLength != layout2.LongLength) { return false; }

            bool equal = true;
            for (int I = 0; I < layout1.Length; I++)
            {
                if (layout1[I] != layout2[I]) { equal = false; break; }
            }
            return equal;
        }

        /// <summary>
        /// Gets an index indicating the offset of the data in a raw PCM stream.
        /// </summary>
        /// <param name="layout">The layout of the channels to retrieve the data offset from</param>
        /// <param name="channel">The channel to retrieve it's data offset.</param>
        /// <returns>A value that indicates data offset for the stream, or -1 indicating that the channel does not exist.</returns>
        public static int GetDataOffset(this ChannelType[] layout , ChannelType channel)
        {
            for (int I = 0; I < layout.Length; I++)
            {
                if (layout[I] == channel) { return I; }
            }
            return -1;
        }

        /// <summary>
        /// Gets a value whether this layout is a stereo layout. To retrieve such a layout use the <see cref="Stereo"/> property.
        /// </summary>
        /// <param name="layout">The layout to test.</param>
        /// <returns>A value whether this layout matches the contents of <see cref="Stereo"/> property.</returns>
        public static System.Boolean IsStereoLayout(this ChannelType[] layout) => IsEqualTo(layout, Stereo);
    }
}
