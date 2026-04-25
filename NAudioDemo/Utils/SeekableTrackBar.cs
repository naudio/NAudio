using System;
using System.Windows.Forms;

namespace NAudioDemo.Utils
{
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
                OnScroll(EventArgs.Empty); // give Scroll handlers a final position to commit on release
                return;
            }
            base.OnMouseUp(e);
        }

        private void SeekToMouseX(int x)
        {
            int track = Math.Max(1, Width - 2 * ThumbInset);
            int clamped = Math.Max(0, Math.Min(track, x - ThumbInset));
            int newValue = Minimum + (int)((double)clamped / track * (Maximum - Minimum));
            if (newValue < Minimum) newValue = Minimum;
            else if (newValue > Maximum) newValue = Maximum;
            if (newValue != Value)
            {
                Value = newValue;
                OnScroll(EventArgs.Empty); // programmatic Value writes don't fire Scroll, so do it explicitly
            }
        }
    }
}
