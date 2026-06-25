using System;
using System.Collections.Generic;

namespace NAudio.SoundFile;

/// <summary>
/// String metadata stored in an audio file (libsndfile
/// <c>sf_get_string</c> / <c>sf_set_string</c>). Availability is
/// format-dependent — FLAC/Ogg/Opus carry Vorbis comments, WAV/AIFF a
/// limited LIST/INFO set; unsupported fields are simply <c>null</c> on
/// read and ignored on write.
/// </summary>
public sealed class SoundFileTags
{
    /// <summary>Track title.</summary>
    public string Title { get; set; }

    /// <summary>Performing artist.</summary>
    public string Artist { get; set; }

    /// <summary>Album / collection name.</summary>
    public string Album { get; set; }

    /// <summary>Free-form comment.</summary>
    public string Comment { get; set; }

    /// <summary>Recording / release date (format is codec-defined).</summary>
    public string Date { get; set; }

    /// <summary>Genre.</summary>
    public string Genre { get; set; }

    /// <summary>Track number.</summary>
    public string TrackNumber { get; set; }

    /// <summary>Copyright.</summary>
    public string Copyright { get; set; }

    /// <summary>Encoding software.</summary>
    public string Software { get; set; }

    /// <summary><c>true</c> if every field is unset.</summary>
    public bool IsEmpty =>
        Title is null && Artist is null && Album is null && Comment is null &&
        Date is null && Genre is null && TrackNumber is null &&
        Copyright is null && Software is null;

    internal IEnumerable<(int Selector, string Value)> NonNull()
    {
        if (Title != null) yield return (SndFileInterop.SF_STR_TITLE, Title);
        if (Artist != null) yield return (SndFileInterop.SF_STR_ARTIST, Artist);
        if (Album != null) yield return (SndFileInterop.SF_STR_ALBUM, Album);
        if (Comment != null) yield return (SndFileInterop.SF_STR_COMMENT, Comment);
        if (Date != null) yield return (SndFileInterop.SF_STR_DATE, Date);
        if (Genre != null) yield return (SndFileInterop.SF_STR_GENRE, Genre);
        if (TrackNumber != null) yield return (SndFileInterop.SF_STR_TRACKNUMBER, TrackNumber);
        if (Copyright != null) yield return (SndFileInterop.SF_STR_COPYRIGHT, Copyright);
        if (Software != null) yield return (SndFileInterop.SF_STR_SOFTWARE, Software);
    }

    internal static SoundFileTags ReadFrom(IntPtr sndfile) => new()
    {
        Title = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_TITLE),
        Artist = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_ARTIST),
        Album = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_ALBUM),
        Comment = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_COMMENT),
        Date = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_DATE),
        Genre = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_GENRE),
        TrackNumber = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_TRACKNUMBER),
        Copyright = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_COPYRIGHT),
        Software = SndFileInterop.GetString(sndfile, SndFileInterop.SF_STR_SOFTWARE)
    };
}
