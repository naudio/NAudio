using System;
using System.Runtime.InteropServices;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI in device
    /// </summary>
    public class MidiIn : IDisposable 
    {
        private IntPtr hMidiIn = IntPtr.Zero;
        private bool disposed = false;
        private MidiInterop.MidiInCallback callback;

        /// <summary>
        /// Called when a MIDI message is received
        /// </summary>
        public event EventHandler<MidiInMessageEventArgs> MessageReceived;

        /// <summary>
        /// An invalid MIDI message
        /// </summary>
        public event EventHandler<MidiInMessageEventArgs> ErrorReceived;

        /// <summary>
        /// Gets the number of MIDI input devices available in the system
        /// </summary>
        public static int NumberOfDevices 
        {
            get 
            {
                return MidiInterop.midiInGetNumDevs();
            }
        }
        
        /// <summary>
        /// Opens a specified MIDI in device
        /// </summary>
        /// <param name="deviceNo">The device number</param>
        public MidiIn(int deviceNo) 
        {
            this.callback = new MidiInterop.MidiInCallback(Callback);
            MmException.Try(MidiInterop.midiInOpen(out hMidiIn, (IntPtr) deviceNo,this.callback,IntPtr.Zero,MidiInterop.CALLBACK_FUNCTION),"midiInOpen");
        }
        
        /// <summary>
        /// Closes this MIDI in device
        /// </summary>
        public void Close() 
        {
            Dispose();
        }

        /// <summary>
        /// Closes this MIDI in device
        /// </summary>
        public void Dispose() 
        {
            GC.KeepAlive(callback);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Start the MIDI in device
        /// </summary>
        public void Start()
        {
            MmException.Try(MidiInterop.midiInStart(hMidiIn), "midiInStart");
        }

        /// <summary>
        /// Stop the MIDI in device
        /// </summary>
        public void Stop()
        {
            MmException.Try(MidiInterop.midiInStop(hMidiIn), "midiInStop");
        }

        /// <summary>
        /// Reset the MIDI in device
        /// </summary>
        public void Reset()
        {
            MmException.Try(MidiInterop.midiInReset(hMidiIn), "midiInReset");
        }
        
        private void Callback(IntPtr midiInHandle, MidiInterop.MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2)
        {
            switch(message)
            {
                case MidiInterop.MidiInMessage.Open:
                    // message Parameter 1 & 2 are not used
                    break;
                case MidiInterop.MidiInMessage.Data:
                    // parameter 1 is packed MIDI message
                    // parameter 2 is milliseconds since MidiInStart
                    if (MessageReceived != null)
                    {
                        MessageReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
                    }
                    break;
                case MidiInterop.MidiInMessage.Error:
                    // parameter 1 is invalid MIDI message
                    if (ErrorReceived != null)
                    {
                        ErrorReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
                    } 
                    break;
                case MidiInterop.MidiInMessage.Close:
                    // message Parameter 1 & 2 are not used
                    break;
                case MidiInterop.MidiInMessage.LongData:
                    // parameter 1 is pointer to MIDI header
                    // parameter 2 is milliseconds since MidiInStart
                    break;
                case MidiInterop.MidiInMessage.LongError:
                    // parameter 1 is pointer to MIDI header
                    // parameter 2 is milliseconds since MidiInStart
                    break;
                case MidiInterop.MidiInMessage.MoreData:
                    // parameter 1 is packed MIDI message
                    // parameter 2 is milliseconds since MidiInStart
                    break;
            }
        }

        /// <summary>
        /// Gets the MIDI in device info
        /// </summary>
        public static MidiInCapabilities DeviceInfo(int midiInDeviceNumber)
        {
            MidiInCapabilities caps = new MidiInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(MidiInterop.midiInGetDevCaps((IntPtr)midiInDeviceNumber,out caps,structSize),"midiInGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Closes the MIDI out device
        /// </summary>
        /// <param name="disposing">True if called from Dispose</param>
        protected virtual void Dispose(bool disposing) 
        {
            if(!this.disposed) 
            {
                //if(disposing) Components.Dispose();
                MidiInterop.midiInClose(hMidiIn);
            }
            disposed = true;
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        ~MidiIn()
        {
            System.Diagnostics.Debug.Assert(false,"MIDI In was not finalised");
            Dispose(false);
        }
    }
}