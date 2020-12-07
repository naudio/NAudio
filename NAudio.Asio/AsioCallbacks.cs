using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// ASIO Callbacks
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AsioCallbacks
    {
        /// <summary>
        /// ASIO Buffer Switch Callback
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AsioBufferSwitchCallBack(int doubleBufferIndex, bool directProcess);
        /// <summary>
        /// ASIO Sample Rate Did Change Callback
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AsioSampleRateDidChangeCallBack(double sRate);
        /// <summary>
        /// ASIO Message Callback
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int AsioAsioMessageCallBack(AsioMessageSelector selector, int value, IntPtr message, IntPtr opt);
        // return AsioTime*
        /// <summary>
        /// ASIO Buffer Switch Time Info Callback
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr AsioBufferSwitchTimeInfoCallBack(IntPtr asioTimeParam, int doubleBufferIndex, bool directProcess);
        //        internal delegate IntPtr AsioBufferSwitchTimeInfoCallBack(ref AsioTime asioTimeParam, int doubleBufferIndex, bool directProcess);

        /// <summary>
        /// Buffer switch callback
        /// void (*bufferSwitch) (long doubleBufferIndex, AsioBool directProcess);
        /// </summary>
        public AsioBufferSwitchCallBack pbufferSwitch;
        /// <summary>
        /// Sample Rate Changed callback
        /// void (*sampleRateDidChange) (AsioSampleRate sRate);
        /// </summary>
        public AsioSampleRateDidChangeCallBack psampleRateDidChange;
        /// <summary>
        /// ASIO Message callback
        /// long (*asioMessage) (long selector, long value, void* message, double* opt);
        /// </summary>
        public AsioAsioMessageCallBack pasioMessage;
        /// <summary>
        /// ASIO Buffer Switch Time Info Callback
        /// AsioTime* (*bufferSwitchTimeInfo) (AsioTime* params, long doubleBufferIndex, AsioBool directProcess);
        /// </summary>
        public AsioBufferSwitchTimeInfoCallBack pbufferSwitchTimeInfo;
    }
}