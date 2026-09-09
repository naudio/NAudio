
using System;
using System.Runtime.InteropServices;

using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.Wave;

public partial class ExtendedAudioFileWriter
{
    private sealed class StreamImpl : ExtendedAudioFileWriter
    {
        private IntPtr streamFileID;
        private GCHandle streamGcHandle;

        public StreamImpl(nint hExtFileObject, IntPtr audioFileId, GCHandle streamHandle, ExtendedAudioFileWriterSettings settings) : base(hExtFileObject, settings)
        {
            this.streamFileID = audioFileId;
            this.streamGcHandle = streamHandle;
        }

        protected override void DisposeNativeData()
        {
            base.DisposeNativeData();
            // Stash the error code until the actual native call
            // is performed; as such, streamFileID is cleared
            // unconditionally and the GC handle is destroyed,
            // even if AudioFileClose reports failure.
            int audioFileCallStatus = 0;
            if (streamFileID != IntPtr.Zero)
            {
                audioFileCallStatus = NativeMethods.AudioFileClose(streamFileID);
                streamFileID = IntPtr.Zero;
            }
            if (streamGcHandle.IsAllocated) { streamGcHandle.Free(); }
            AudioFileException.ThrowIfError(audioFileCallStatus);
        }
    }
}