namespace NAudio.Wave
{
    /// <summary>Defines different channel types that may exist in an audio stream.</summary>
    public enum ChannelType : byte
    {
        /// <summary>The left speaker.</summary>
        Left,
        /// <summary>The right speaker.</summary>
        Right,
        /// <summary>The central speaker. Also known as the 'voice' speaker in terms of cinemas.</summary>
        Center,
        /// <summary>The low-frequency speaker (subwoofer).</summary>
        LowFrequency,
        /// <summary>The speaker that is back and left.</summary>
        BackLeft,
        /// <summary>The speaker that is back and right.</summary>
        BackRight,
        /// <summary>The speaker that is located at the front and left from the center speaker.</summary>
        FrontLeftOfCenter,
        /// <summary>The speaker that is located at the front and right from the center speaker.</summary>
        FrontRightOfCenter,
        /// <summary>The center speaker that is located at the opposite direction of the <see cref="Center"/> one.</summary>
        BackCenter,
        /// <summary>The left-side speaker.</summary>
        SideLeft,
        /// <summary>The right-side speaker.</summary>
        SideRight,
        /// <summary>The top-center speaker.</summary>
        TopCenter,
        /// <summary>The top-front left speaker.</summary>
        TopFrontLeft,
        /// <summary>The top-front right speaker.</summary>
        TopFrontRight,
        /// <summary>The top-front center speaker.</summary>
        TopFrontCenter,
        /// <summary>The top-back left speaker.</summary>
        TopBackLeft,
        /// <summary>The top-back right speaker.</summary>
        TopBackRight,
        /// <summary>The top-back center speaker.</summary>
        TopBackCenter
    }
}
