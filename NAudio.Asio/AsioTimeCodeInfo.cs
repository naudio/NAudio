namespace NAudio.Wave
{
    /// <summary>
    /// SMPTE/MTC time code reported by the ASIO driver for the current buffer. Surfaced on
    /// <see cref="AsioProcessBuffers.TimeCode"/> and <see cref="AsioAudioCapturedEventArgs.TimeCode"/>
    /// when the driver is receiving an external time-code source (LTC input, MTC over MIDI, etc.).
    /// </summary>
    /// <remarks>
    /// Only present when the driver returns a valid time code for the buffer; the surfacing properties
    /// are typed as <c>AsioTimeCodeInfo?</c> and will be <c>null</c> in the common case where no
    /// external time-code source is connected.
    /// </remarks>
    public readonly struct AsioTimeCodeInfo
    {
        /// <summary>Time code position, in samples since the start of the time-code stream.</summary>
        public long Samples { get; init; }

        /// <summary>Time-code speed (1.0 = nominal). Often 1.0 even when audio is varispeeding.</summary>
        public double Speed { get; init; }

        /// <summary>True if the time-code source is running (transport playing).</summary>
        public bool Running { get; init; }

        /// <summary>True if the time-code source is running in reverse.</summary>
        public bool Reverse { get; init; }

        /// <summary>True if the time-code source is locked at nominal speed.</summary>
        public bool OnSpeed { get; init; }

        /// <summary>True if the time-code source has paused (no advancement).</summary>
        public bool Still { get; init; }
    }
}
