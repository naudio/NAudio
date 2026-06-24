using System.Globalization;

namespace NAudio.Sfz
{
    /// <summary>
    /// Parses SFZ key values, which may be a MIDI note number (0–127) or a note
    /// name such as <c>c4</c>, <c>c#3</c> or <c>db5</c> — case-insensitive, in
    /// both the letter and the flat (<c>Db4</c>, <c>dB4</c> and <c>DB4</c> all
    /// equal <c>db4</c>). SFZ uses the convention that middle C (<c>c4</c>) is
    /// MIDI note 60, so <c>c-1</c> is 0.
    /// </summary>
    public static class SfzNoteName
    {
        // semitone offset within an octave for each natural note letter
        private static readonly int[] LetterSemitone = { 9, 11, 0, 2, 4, 5, 7 }; // a b c d e f g

        /// <summary>
        /// Tries to parse a key value (note number or note name) to a MIDI note
        /// number. Returns false if the text is neither.
        /// </summary>
        public static bool TryParse(string text, out int note)
        {
            note = 0;
            if (string.IsNullOrEmpty(text)) return false;

            text = text.Trim();
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out note))
                return true;

            int i = 0;
            char letter = char.ToLowerInvariant(text[i]);
            if (letter < 'a' || letter > 'g') return false;
            int semitone = LetterSemitone[letter - 'a'];
            i++;

            // the accidental is case-insensitive like the note letter ('B' = 'b')
            char accidental = i < text.Length ? char.ToLowerInvariant(text[i]) : '\0';
            if (accidental == '#' || accidental == 'b')
            {
                semitone += accidental == '#' ? 1 : -1;
                i++;
            }

            if (i >= text.Length) return false;
            if (!int.TryParse(text.Substring(i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int octave))
                return false;

            // c4 = 60  =>  midi = (octave + 1) * 12 + semitone
            note = (octave + 1) * 12 + semitone;
            return note >= 0 && note <= 127;
        }

        /// <summary>
        /// Parses a key value to a MIDI note number, returning
        /// <paramref name="fallback"/> if it is neither a number nor a note name.
        /// </summary>
        public static int Parse(string text, int fallback) =>
            TryParse(text, out var note) ? note : fallback;
    }
}
