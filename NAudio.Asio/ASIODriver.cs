using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Main AsioDriver Class. To use this class, you need to query first the GetAsioDriverNames() and
    /// then use the GetAsioDriverByName to instantiate the correct AsioDriver.
    /// This is the first AsioDriver binding fully implemented in C#!
    /// 
    /// Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioDriver
    {
        private IntPtr pAsioComObject;
        private IntPtr pinnedcallbacks;
        private AsioDriverVTable asioDriverVTable;

        private AsioDriver()
        {
        }

        /// <summary>
        /// Gets the ASIO driver names installed.
        /// </summary>
        /// <returns>a list of driver names. Use this name to GetAsioDriverByName</returns>
        public static string[] GetAsioDriverNames()
        {
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");
            var names = new string[0];
            if (regKey != null)
            {
                names = regKey.GetSubKeyNames();
                regKey.Close();
            }
            return names;
        }

        /// <summary>
        /// Instantiate a AsioDriver given its name.
        /// </summary>
        /// <param name="name">The name of the driver</param>
        /// <returns>an AsioDriver instance</returns>
        public static AsioDriver GetAsioDriverByName(String name)
        {
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO\\" + name);
            if (regKey == null)
            {
                throw new ArgumentException($"Driver Name {name} doesn't exist");
            }
            var guid = regKey.GetValue("CLSID").ToString();
            return GetAsioDriverByGuid(new Guid(guid));
        }

        /// <summary>
        /// Instantiate the ASIO driver by GUID.
        /// </summary>
        /// <param name="guid">The GUID.</param>
        /// <returns>an AsioDriver instance</returns>
        public static AsioDriver GetAsioDriverByGuid(Guid guid)
        {
            var driver = new AsioDriver();
            driver.InitFromGuid(guid);
            return driver;
        }

        /// <summary>
        /// Inits the AsioDriver..
        /// </summary>
        /// <param name="sysHandle">The sys handle.</param>
        /// <returns></returns>
        public bool Init(IntPtr sysHandle)
        {
            int ret = asioDriverVTable.init(pAsioComObject, sysHandle);
            return ret == 1;
        }

        /// <summary>
        /// Gets the name of the driver.
        /// </summary>
        /// <returns></returns>
        public string GetDriverName() 
        {
            var name = new StringBuilder(256);
            asioDriverVTable.getDriverName(pAsioComObject, name);
            return name.ToString();
        }

        /// <summary>
        /// Gets the driver version.
        /// </summary>
        /// <returns></returns>
        public int GetDriverVersion() {
            return asioDriverVTable.getDriverVersion(pAsioComObject);
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage()
        {
            var errorMessage = new StringBuilder(256);
            asioDriverVTable.getErrorMessage(pAsioComObject, errorMessage);
            return errorMessage.ToString();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            HandleException(asioDriverVTable.start(pAsioComObject),"start");
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public AsioError Stop()
        {
            return asioDriverVTable.stop(pAsioComObject);
        }

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        /// <param name="numInputChannels">The num input channels.</param>
        /// <param name="numOutputChannels">The num output channels.</param>
        public void GetChannels(out int numInputChannels, out int numOutputChannels)
        {
            HandleException(asioDriverVTable.getChannels(pAsioComObject, out numInputChannels, out numOutputChannels), "getChannels");
        }

        /// <summary>
        /// Gets the latencies (n.b. does not throw an exception)
        /// </summary>
        /// <param name="inputLatency">The input latency.</param>
        /// <param name="outputLatency">The output latency.</param>
        public AsioError GetLatencies(out int inputLatency, out int outputLatency)
        {
            return asioDriverVTable.getLatencies(pAsioComObject, out inputLatency, out outputLatency);
        }

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <param name="minSize">Size of the min.</param>
        /// <param name="maxSize">Size of the max.</param>
        /// <param name="preferredSize">Size of the preferred.</param>
        /// <param name="granularity">The granularity.</param>
        public void GetBufferSize(out int minSize, out int maxSize, out int preferredSize, out int granularity)
        {
            HandleException(asioDriverVTable.getBufferSize(pAsioComObject, out minSize, out maxSize, out preferredSize, out granularity), "getBufferSize");
        }

        /// <summary>
        /// Determines whether this instance can use the specified sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can sample rate] the specified sample rate; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSampleRate(double sampleRate)
        {
            var error = asioDriverVTable.canSampleRate(pAsioComObject, sampleRate);
            if (error == AsioError.ASE_NoClock)
            {
                return false;
            } 
            if ( error == AsioError.ASE_OK )
            {
                return true;
            }
            HandleException(error, "canSampleRate");
            return false;
        }

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        /// <returns></returns>
        public double GetSampleRate()
        {
            double sampleRate;
            HandleException(asioDriverVTable.getSampleRate(pAsioComObject, out sampleRate), "getSampleRate");
            return sampleRate;
        }

        /// <summary>
        /// Sets the sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        public void SetSampleRate(double sampleRate)
        {
            HandleException(asioDriverVTable.setSampleRate(pAsioComObject, sampleRate), "setSampleRate");
        }

        /// <summary>
        /// Gets the clock sources.
        /// </summary>
        /// <param name="clocks">The clocks.</param>
        /// <param name="numSources">The num sources.</param>
        public void GetClockSources(out long clocks, int numSources)
        {
            HandleException(asioDriverVTable.getClockSources(pAsioComObject, out clocks,numSources), "getClockSources");
        }

        /// <summary>
        /// Sets the clock source.
        /// </summary>
        /// <param name="reference">The reference.</param>
        public void SetClockSource(int reference)
        {
            HandleException(asioDriverVTable.setClockSource(pAsioComObject, reference), "setClockSources");
        }

        /// <summary>
        /// Gets the sample position.
        /// </summary>
        /// <param name="samplePos">The sample pos.</param>
        /// <param name="timeStamp">The time stamp.</param>
        public void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp)
        {
            HandleException(asioDriverVTable.getSamplePosition(pAsioComObject, out samplePos, ref timeStamp), "getSamplePosition");
        }

        /// <summary>
        /// Gets the channel info.
        /// </summary>
        /// <param name="channelNumber">The channel number.</param>
        /// <param name="trueForInputInfo">if set to <c>true</c> [true for input info].</param>
        /// <returns>Channel Info</returns>
        public AsioChannelInfo GetChannelInfo(int channelNumber, bool trueForInputInfo)
        {
            var info = new AsioChannelInfo {channel = channelNumber, isInput = trueForInputInfo};
            HandleException(asioDriverVTable.getChannelInfo(pAsioComObject, ref info), "getChannelInfo");
            return info;
        }

        /// <summary>
        /// Creates the buffers.
        /// </summary>
        /// <param name="bufferInfos">The buffer infos.</param>
        /// <param name="numChannels">The num channels.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="callbacks">The callbacks.</param>
        public void CreateBuffers(IntPtr bufferInfos, int numChannels, int bufferSize, ref AsioCallbacks callbacks)
        {
            // next two lines suggested by droidi on codeplex issue tracker
            pinnedcallbacks = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, pinnedcallbacks, false);
            HandleException(asioDriverVTable.createBuffers(pAsioComObject, bufferInfos, numChannels, bufferSize, pinnedcallbacks), "createBuffers");
        }

        /// <summary>
        /// Disposes the buffers.
        /// </summary>
        public AsioError DisposeBuffers()
        {
            AsioError result = asioDriverVTable.disposeBuffers(pAsioComObject);
            Marshal.FreeHGlobal(pinnedcallbacks);
            return result;
        }

        /// <summary>
        /// Controls the panel.
        /// </summary>
        public void ControlPanel()
        {
            HandleException(asioDriverVTable.controlPanel(pAsioComObject), "controlPanel");
        }

        /// <summary>
        /// Futures the specified selector.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="opt">The opt.</param>
        public void Future(int selector, IntPtr opt)
        {
            HandleException(asioDriverVTable.future(pAsioComObject, selector, opt), "future");
        }

        /// <summary>
        /// Notifies OutputReady to the AsioDriver.
        /// </summary>
        /// <returns></returns>
        public AsioError OutputReady()
        {
            return asioDriverVTable.outputReady(pAsioComObject);
        }

        /// <summary>
        /// Releases this instance.
        /// </summary>
        public void ReleaseComAsioDriver()
        {
            Marshal.Release(pAsioComObject);
        }

        /// <summary>
        /// Handles the exception. Throws an exception based on the error.
        /// </summary>
        /// <param name="error">The error to check.</param>
        /// <param name="methodName">Method name</param>
        private void HandleException(AsioError error, string methodName)
        {
            if (error != AsioError.ASE_OK && error != AsioError.ASE_SUCCESS)
            {
                var asioException = new AsioException(
                    $"Error code [{AsioException.getErrorName(error)}] while calling ASIO method <{methodName}>, {this.GetErrorMessage()}");
                asioException.Error = error;
                throw asioException;
            }
        }

        /// <summary>
        /// Inits the vTable method from GUID. This is a tricky part of this class.
        /// </summary>
        /// <param name="asioGuid">The ASIO GUID.</param>
        private void InitFromGuid(Guid asioGuid)
        {
            const uint CLSCTX_INPROC_SERVER = 1;
            // Start to query the virtual table a index 3 (init method of AsioDriver)
            const int INDEX_VTABLE_FIRST_METHOD = 3;

            // Pointer to the ASIO object
            // USE CoCreateInstance instead of builtin COM-Class instantiation,
            // because the AsioDriver expect to have the ASIOGuid used for both COM Object and COM interface
            // The CoCreateInstance is working only in STAThread mode.
            int hresult = CoCreateInstance(ref asioGuid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref asioGuid, out pAsioComObject);
            if ( hresult != 0 )
            {
                throw new COMException("Unable to instantiate ASIO. Check if STAThread is set",hresult);
            }

            // The first pointer at the adress of the ASIO Com Object is a pointer to the
            // C++ Virtual table of the object.
            // Gets a pointer to VTable.
            IntPtr pVtable = Marshal.ReadIntPtr(pAsioComObject);

            // Instantiate our Virtual table mapping
            asioDriverVTable = new AsioDriverVTable();

            // This loop is going to retrieve the pointer from the C++ VirtualTable
            // and attach an internal delegate in order to call the method on the COM Object.
            FieldInfo[] fieldInfos =  typeof (AsioDriverVTable).GetFields();
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                FieldInfo fieldInfo = fieldInfos[i];
                // Read the method pointer from the VTable
                IntPtr pPointerToMethodInVTable = Marshal.ReadIntPtr(pVtable, (i + INDEX_VTABLE_FIRST_METHOD) * IntPtr.Size);
                // Instantiate a delegate
                object methodDelegate = Marshal.GetDelegateForFunctionPointer(pPointerToMethodInVTable, fieldInfo.FieldType);
                // Store the delegate in our C# VTable
                fieldInfo.SetValue(asioDriverVTable, methodDelegate);
            }
        }

        /// <summary>
        /// Internal VTable structure to store all the delegates to the C++ COM method.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private class AsioDriverVTable
        {
            //3  virtual ASIOBool init(void *sysHandle) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate int ASIOInit(IntPtr _pUnknown, IntPtr sysHandle);
            public ASIOInit init = null;
            //4  virtual void getDriverName(char *name) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate void ASIOgetDriverName(IntPtr _pUnknown, StringBuilder name);
            public ASIOgetDriverName getDriverName = null;
            //5  virtual long getDriverVersion() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate int ASIOgetDriverVersion(IntPtr _pUnknown);
            public ASIOgetDriverVersion getDriverVersion = null;
            //6  virtual void getErrorMessage(char *string) = 0;	
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate void ASIOgetErrorMessage(IntPtr _pUnknown, StringBuilder errorMessage);
            public ASIOgetErrorMessage getErrorMessage = null;
            //7  virtual ASIOError start() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOstart(IntPtr _pUnknown);
            public ASIOstart start = null;
            //8  virtual ASIOError stop() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOstop(IntPtr _pUnknown);
            public ASIOstop stop = null;
            //9  virtual ASIOError getChannels(long *numInputChannels, long *numOutputChannels) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetChannels(IntPtr _pUnknown, out int numInputChannels, out int numOutputChannels);
            public ASIOgetChannels getChannels = null;
            //10  virtual ASIOError getLatencies(long *inputLatency, long *outputLatency) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetLatencies(IntPtr _pUnknown, out int inputLatency, out int outputLatency);
            public ASIOgetLatencies getLatencies = null;
            //11 virtual ASIOError getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetBufferSize(IntPtr _pUnknown, out int minSize, out int maxSize, out int preferredSize, out int granularity);
            public ASIOgetBufferSize getBufferSize = null;
            //12 virtual ASIOError canSampleRate(ASIOSampleRate sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOcanSampleRate(IntPtr _pUnknown, double sampleRate);
            public ASIOcanSampleRate canSampleRate = null;
            //13 virtual ASIOError getSampleRate(ASIOSampleRate *sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetSampleRate(IntPtr _pUnknown, out double sampleRate);
            public ASIOgetSampleRate getSampleRate = null;
            //14 virtual ASIOError setSampleRate(ASIOSampleRate sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOsetSampleRate(IntPtr _pUnknown, double sampleRate);
            public ASIOsetSampleRate setSampleRate = null;
            //15 virtual ASIOError getClockSources(ASIOClockSource *clocks, long *numSources) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetClockSources(IntPtr _pUnknown, out long clocks, int numSources);
            public ASIOgetClockSources getClockSources = null;
            //16 virtual ASIOError setClockSource(long reference) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOsetClockSource(IntPtr _pUnknown, int reference);
            public ASIOsetClockSource setClockSource = null;
            //17 virtual ASIOError getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetSamplePosition(IntPtr _pUnknown, out long samplePos, ref Asio64Bit timeStamp);
            public ASIOgetSamplePosition getSamplePosition = null;
            //18 virtual ASIOError getChannelInfo(ASIOChannelInfo *info) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetChannelInfo(IntPtr _pUnknown, ref AsioChannelInfo info);
            public ASIOgetChannelInfo getChannelInfo = null;
            //19 virtual ASIOError createBuffers(ASIOBufferInfo *bufferInfos, long numChannels, long bufferSize, ASIOCallbacks *callbacks) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            //            public delegate ASIOError ASIOcreateBuffers(IntPtr _pUnknown, ref ASIOBufferInfo[] bufferInfos, int numChannels, int bufferSize, ref ASIOCallbacks callbacks);
            public delegate AsioError ASIOcreateBuffers(IntPtr _pUnknown, IntPtr bufferInfos, int numChannels, int bufferSize, IntPtr callbacks);
            public ASIOcreateBuffers createBuffers = null;
            //20 virtual ASIOError disposeBuffers() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOdisposeBuffers(IntPtr _pUnknown);
            public ASIOdisposeBuffers disposeBuffers = null;
            //21 virtual ASIOError controlPanel() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOcontrolPanel(IntPtr _pUnknown);
            public ASIOcontrolPanel controlPanel = null;
            //22 virtual ASIOError future(long selector,void *opt) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOfuture(IntPtr _pUnknown, int selector, IntPtr opt);
            public ASIOfuture future = null;
            //23 virtual ASIOError outputReady() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOoutputReady(IntPtr _pUnknown);
            public ASIOoutputReady outputReady = null;
        }

        [DllImport("ole32.Dll")]
        private static extern int CoCreateInstance(ref Guid clsid,
           IntPtr inner,
           uint context,
           ref Guid uuid,
           out IntPtr rReturnedComObject);
    }
}
