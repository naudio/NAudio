using System;
using System.Windows.Forms;

namespace NAudioDemo.Utils;

/// <summary>
/// A <see cref="TrackBar"/> that behaves like a media seek bar:
/// clicking anywhere on the track jumps the thumb to that exact position
/// (instead of the default <see cref="TrackBar.LargeChange"/> step), and
/// <see cref="IsScrubbing"/> is <c>true</c> while the user is interacting
/// with the mouse so an external position timer can stop fighting them.
/// </summary>
public class SeekableTrackBar : TrackBar
{
    // Approximate inset for the default TrackBar thumb on either side of the track.
    // Good enough for the seek-to-click feel; perfect pixel accuracy would require P/Invoking TBM_GETTHUMBLENGTH.
    private const int ThumbInset = 13;

    // The last value we raised Scroll for, so a single click (MouseDown + MouseUp both want to
    // commit) or a drag that pauses on one value doesn't fire two seeks to the same position.
    // Two seeks to the same point queue the post-seek audio twice and play it back doubled.
    private int? lastCommittedValue;

    /// <summary>
    /// True while the user is mid-drag with the left mouse button down. External code
    /// (e.g. a position-update timer) should skip writing <see cref="TrackBar.Value"/> while this is true.
    /// </summary>
    public bool IsScrubbing { get; private set; }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            IsScrubbing = true;
            Capture = true;
            lastCommittedValue = null; // start a fresh interaction
            SeekToMouseX(e.X);
            Focus();
            return; // suppress default LargeChange-jump behaviour
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (IsScrubbing && (e.Button & MouseButtons.Left) == MouseButtons.Left)
        {
            SeekToMouseX(e.X);
            return;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (IsScrubbing)
        {
            IsScrubbing = false;
            Capture = false;
            base.OnMouseUp(e);
            Commit(); // commit the final position on release (no-op if MouseDown/Move already did)
            return;
        }
        base.OnMouseUp(e);
    }

    private void SeekToMouseX(int x)
    {
        int track = Math.Max(1, Width - 2 * ThumbInset);
        int clamped = Math.Max(0, Math.Min(track, x - ThumbInset));
        int newValue = Math.Clamp(Minimum + (int)((double)clamped / track * (Maximum - Minimum)), Minimum, Maximum);
        if (newValue != Value)
        {
            Value = newValue;
        }
        Commit();
    }

    private void Commit()
    {
        // Programmatic Value writes don't raise Scroll, so we raise it explicitly - but only once
        // per distinct position, so a single click doesn't seek twice to the same place.
        if (lastCommittedValue == Value) return;
        lastCommittedValue = Value;
        OnScroll(EventArgs.Empty);
    }
}
