namespace NAudio.MediaFoundation
{
    public enum MF_OBJECT_TYPE
    {
        MF_OBJECT_MEDIASOURCE = 0,
        MF_OBJECT_BYTESTREAM = (MF_OBJECT_MEDIASOURCE + 1),
        MF_OBJECT_INVALID = (MF_OBJECT_BYTESTREAM + 1)
    }
}