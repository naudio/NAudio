
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
            if (streamFileID != IntPtr.Zero)
            {
                AudioFileException.ThrowIfError(
                    NativeMethods.AudioFileClose(streamFileID)
                );
                streamFileID = IntPtr.Zero;
            }
            if (streamGcHandle.IsAllocated) { streamGcHandle.Free(); }
        }
    }
}