#region Original License
//Widows Media Format Interfaces
//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER
//  REMAINS UNCHANGED.
//
//  Email:  yetiicb@hotmail.com
//
//  Copyright (C) 2002-2004 Idael Cardoso.
//
#endregion

#region Code Modifications Note
// Yuval Naveh, 2010
// Note - The code below has been changed and fixed from its original form.
// Changes include - Formatting, Layout, Coding standards and removal of compilation warnings

// Mark Heath, 2010 - modified for inclusion in NAudio
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;

namespace NAudio.WindowsMediaFormat
{
    /// <summary>
    /// Helper to encapsulate IWMStreamConfig.
    /// </summary>
    public class WMStreamConfig
    {
        private readonly IWMStreamConfig streamConfig;

        /// <summary>
        /// WMStreamConfig constructor
        /// </summary>
        /// <param name="config">IWMStreamConfig to wrap</param>
        public WMStreamConfig(IWMStreamConfig config)
        {
            streamConfig = config;
        }

        /// <summary>
        /// Gets the waveformat of this stream
        /// </summary>
        /// <returns>A waveformat (or null if this is not an audio stream)</returns>
        public WaveFormat GetWaveFormat()
        {
            var props = (IWMMediaProps) streamConfig;
            int size = Math.Max(512, Marshal.SizeOf(typeof (WM_MEDIA_TYPE)) + Marshal.SizeOf(typeof (WaveFormat)));
            IntPtr buffer = Marshal.AllocCoTaskMem(size);
            try
            {
                props.GetMediaType(buffer, ref size);
                var mt = (WM_MEDIA_TYPE)Marshal.PtrToStructure(buffer, typeof(WM_MEDIA_TYPE));
                if ((mt.majortype == MediaTypes.WMMEDIATYPE_Audio) &&
                        // n.b. subtype may not be PCM, but some variation of WM Audio
                        (mt.formattype == MediaTypes.WMFORMAT_WaveFormatEx))
                {
                    var fmt = new WaveFormatExtensible(44100, 16, 2);
                    Marshal.PtrToStructure(mt.pbFormat, fmt);
                    return fmt;
                }
                return null; 
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
        }


        /// <summary>
        /// Wrapped IWMStreamConfig object
        /// </summary>
        public IWMStreamConfig StreamConfig
        {
            get { return streamConfig; }
        }

        /// <summary>
        /// Read/Write. Bitrate of the stream. Wraps IWMStreamConfig.GetBitrate and WMStreamConfig.SetBitrate
        /// </summary>
        public uint Bitrate
        {
            get
            {
                uint res;
                streamConfig.GetBitrate(out res);
                return res;
            }
            set
            {
                streamConfig.SetBitrate(value);
            }
        }

        /// <summary>
        /// Get/Set the buffer window of the stream. Wraps IWMStreamConfig.GetBufferWindow and IWMStreamConfig.SetBufferWindow
        /// </summary>
        public uint BufferWindow
        {
            get
            {
                uint res;
                streamConfig.GetBufferWindow(out res);
                return res;
            }
            set
            {
                streamConfig.SetBufferWindow(value);
            }
        }

        /// <summary>
        /// Get/Set commention name. Wraps IWMStreamConfig.GetConnectionName and IWMStreamConfig.SetConnectionName
        /// </summary>
        public string ConnectionName
        {
            get
            {
                StringBuilder name;
                ushort namelen = 0;
                streamConfig.GetConnectionName(null, ref namelen);
                name = new StringBuilder(namelen);
                streamConfig.GetConnectionName(name, ref namelen);
                return name.ToString();
            }
            set
            {
                streamConfig.SetConnectionName(value);
            }
        }

        /// <summary>
        /// Get/Set stream name. Wraps IWMStreamConfig.GetStreamName and IWMStreamConfig.SetStreamName
        /// </summary>
        public string StreamName
        {
            get
            {
                StringBuilder name;
                ushort namelen = 0;
                streamConfig.GetStreamName(null, ref namelen);
                name = new StringBuilder(namelen);
                streamConfig.GetStreamName(name, ref namelen);
                return name.ToString();
            }
            set
            {
                streamConfig.SetStreamName(value);
            }
        }

        /// <summary>
        /// Get/Set stream number. Wraps IWMStreamConfig.GetStreamNumber and IWMStreamConfig.SetStreamNumber
        /// </summary>
        public ushort StreamNumber
        {
            get
            {
                ushort res;
                streamConfig.GetStreamNumber(out res);
                return res;
            }
            set
            {
                streamConfig.SetStreamNumber(value);
            }
        }

        /// <summary>
        /// Get the stream type (GUID that correspons of WM_MEDIA_TYPE.majortype
        /// Wraps IWMStreamConfig.GetStreamType 
        /// </summary>
        public Guid StreamType
        {
            get
            {
                Guid res;
                streamConfig.GetStreamType(out res);
                return res;
            }
        }
    }

}
