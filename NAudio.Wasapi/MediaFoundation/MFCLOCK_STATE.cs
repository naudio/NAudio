namespace NAudio.MediaFoundation
{
    public enum MFCLOCK_STATE
    {
        INVALID = 0,
        RUNNING = (INVALID + 1),
        STOPPED = (RUNNING + 1),
        PAUSED = (STOPPED + 1)
    }
}