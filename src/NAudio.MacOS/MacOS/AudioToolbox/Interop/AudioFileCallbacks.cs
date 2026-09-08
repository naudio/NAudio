
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace NAudio.MacOS.AudioToolbox.Interop;

internal static unsafe class AudioFileCallbacks
{
    public sealed class CallbacksUserData
    {
        public const int CustomErrorConst = -84993;

        private Exception exception;
        public readonly Stream Stream;

        public CallbacksUserData(Stream stream)
        {
            Stream = stream;
            exception = null;
        }

        public Exception Exception => exception;

        public void AssignException(Exception ex) => exception = ex;
    }

    public static readonly AudioFile_ReadProc ReadProcedure = &ReadProcImpl;

    public static readonly AudioFile_WriteProc WriteProcedure = &WriteProcImpl;

    public static readonly AudioFile_GetSizeProc GetSizeProcedure = &GetSizeProcImpl;

    public static readonly AudioFile_SetSizeProc SetSizeProcedure = &SetSizeProcImpl;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int ReadProcImpl(
        System.IntPtr inClientData,
        long inPosition,
        uint requestCount,
        nint buffer,
        uint* actualCount
    )
    {
        *actualCount = 0U;
        // Note: We want this to throw an unhandled exception;
        // because this is not accessed through any public API
        // and if this crashes, it would suggest to us that
        // we are doing something really wrong.
        // So, it is actually correct to terminate the process immediately.
        CallbacksUserData ud = (CallbacksUserData)GCHandle.FromIntPtr(inClientData).Target;

        /*
            Note: In these callbacks implementations, we allow the AudioFile subsystem
            to request more than int.MaxValue bytes, but we automatically 
            cap it to int.MaxValue so that the .NET call does not fail but it will
            return less than requested bytes, which is not a problem because
            the AudioFile can request again if it needs more data.
            The same does apply for writing.
        */

        try
        {
            if (inPosition > ud.Stream.Length || inPosition < 0L)
            {
                return AudioFileErrors.kAudioFilePositionError;
            }
            else if (inPosition != ud.Stream.Position)
            {
                ud.Stream.Position = inPosition;
            }

            int bytesRead = ud.Stream.Read(new Span<byte>(
                buffer.ToPointer(),
                requestCount > int.MaxValue ? int.MaxValue : (int)requestCount
            ));

            // Safe cast; Will always be in [0..int.MaxValue].
            *actualCount = (uint)bytesRead;
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException)
            {
                return AudioFileErrors.kAudioFileNotOpenError;
            }
            else if (ex is NotSupportedException)
            {
                return AudioFileErrors.kAudioFileOperationNotSupportedError;
            }
            else
            {
                ud.AssignException(ex);
                return CallbacksUserData.CustomErrorConst;
            }
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int WriteProcImpl(
        IntPtr inClientData,
        long inPosition,
        uint requestCount,
        nint buffer,
        uint* actualCount
    )
    {
        *actualCount = 0U;
        CallbacksUserData ud = (CallbacksUserData)GCHandle.FromIntPtr(inClientData).Target;

        try
        {
            if (inPosition < 0L)
            {
                return AudioFileErrors.kAudioFilePositionError;
            }
            else if (inPosition != ud.Stream.Position)
            {
                ud.Stream.Position = inPosition;
            }

            var actualWriteCount = requestCount > int.MaxValue ? int.MaxValue : (int)requestCount;

            ud.Stream.Write(new ReadOnlySpan<byte>(
                buffer.ToPointer(),
                actualWriteCount
            ));

            // Safe cast; Will always be in [0..int.MaxValue].
            *actualCount = (uint)actualWriteCount;
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException)
            {
                return AudioFileErrors.kAudioFileNotOpenError;
            }
            else if (ex is NotSupportedException)
            {
                return AudioFileErrors.kAudioFileOperationNotSupportedError;
            }
            else
            {
                ud.AssignException(ex);
                return CallbacksUserData.CustomErrorConst;
            }
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static long GetSizeProcImpl(IntPtr inClientData)
    {
        CallbacksUserData ud = (CallbacksUserData)GCHandle.FromIntPtr(inClientData).Target;

        try
        {
            return ud.Stream.Length;
        }
        catch (Exception ex)
        {
            ud.AssignException(ex);
            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int SetSizeProcImpl(IntPtr inClientData, long size)
    {
        CallbacksUserData ud = (CallbacksUserData)GCHandle.FromIntPtr(inClientData).Target;

        try
        {
            ud.Stream.SetLength(size);
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException)
            {
                return AudioFileErrors.kAudioFileNotOpenError;
            }
            else if (ex is NotSupportedException)
            {
                return AudioFileErrors.kAudioFileOperationNotSupportedError;
            }
            else
            {
                ud.AssignException(ex);
                return CallbacksUserData.CustomErrorConst;
            }
        }
        return 0;
    }

}