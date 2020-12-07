﻿// -----------------------------------------
// SoundScribe (TM) and related software.
// 
// Copyright (C) 2007-2011 Vannatech
// http://www.vannatech.com
// All rights reserved.
// 
// This source code is subject to the MIT License.
// http://www.opensource.org/licenses/mit-license.php
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------
// milligan22963 - ported to nAudio
// -----------------------------------------

using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IAudioSessionControl interface
    /// Defined in AudioPolicy.h
    /// </summary>
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionControl
    {
        /// <summary>
        /// Retrieves the current state of the audio session.
        /// </summary>
        /// <param name="state">Receives the current session state.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetState(
            [Out] out AudioSessionState state);

        /// <summary>
        /// Retrieves the display name for the audio session.
        /// </summary>
        /// <param name="displayName">Receives a string that contains the display name.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetDisplayName(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string displayName);

        /// <summary>
        /// Assigns a display name to the current audio session.
        /// </summary>
        /// <param name="displayName">A string that contains the new display name for the session.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int SetDisplayName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string displayName,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Retrieves the path for the display icon for the audio session.
        /// </summary>
        /// <param name="iconPath">Receives a string that specifies the fully qualified path of the file that contains the icon.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetIconPath(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

        /// <summary>
        /// Assigns a display icon to the current session.
        /// </summary>
        /// <param name="iconPath">A string that specifies the fully qualified path of the file that contains the new icon.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int SetIconPath(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Retrieves the grouping parameter of the audio session.
        /// </summary>
        /// <param name="groupingId">Receives the grouping parameter ID.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetGroupingParam(
            [Out] out Guid groupingId);

        /// <summary>
        /// Assigns a session to a grouping of sessions.
        /// </summary>
        /// <param name="groupingId">The new grouping parameter ID.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int SetGroupingParam(
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid groupingId,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Registers the client to receive notifications of session events, including changes in the session state.
        /// </summary>
        /// <param name="client">A client-implemented <see cref="IAudioSessionEvents"/> interface.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int RegisterAudioSessionNotification(
            [In] IAudioSessionEvents client);

        /// <summary>
        /// Deletes a previous registration by the client to receive notifications.
        /// </summary>
        /// <param name="client">A client-implemented <see cref="IAudioSessionEvents"/> interface.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int UnregisterAudioSessionNotification(
            [In] IAudioSessionEvents client);
    }


    /// <summary>
    /// Windows CoreAudio IAudioSessionControl interface
    /// Defined in AudioPolicy.h
    /// </summary>
    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionControl2 : IAudioSessionControl
    {
        /// <summary>
        /// Retrieves the current state of the audio session.
        /// </summary>
        /// <param name="state">Receives the current session state.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int GetState(
            [Out] out AudioSessionState state);

        /// <summary>
        /// Retrieves the display name for the audio session.
        /// </summary>
        /// <param name="displayName">Receives a string that contains the display name.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int GetDisplayName(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string displayName);

        /// <summary>
        /// Assigns a display name to the current audio session.
        /// </summary>
        /// <param name="displayName">A string that contains the new display name for the session.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int SetDisplayName(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string displayName,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Retrieves the path for the display icon for the audio session.
        /// </summary>
        /// <param name="iconPath">Receives a string that specifies the fully qualified path of the file that contains the icon.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int GetIconPath(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string iconPath);

        /// <summary>
        /// Assigns a display icon to the current session.
        /// </summary>
        /// <param name="iconPath">A string that specifies the fully qualified path of the file that contains the new icon.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int SetIconPath(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Retrieves the grouping parameter of the audio session.
        /// </summary>
        /// <param name="groupingId">Receives the grouping parameter ID.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int GetGroupingParam(
            [Out] out Guid groupingId);

        /// <summary>
        /// Assigns a session to a grouping of sessions.
        /// </summary>
        /// <param name="groupingId">The new grouping parameter ID.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int SetGroupingParam(
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid groupingId,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        /// <summary>
        /// Registers the client to receive notifications of session events, including changes in the session state.
        /// </summary>
        /// <param name="client">A client-implemented <see cref="IAudioSessionEvents"/> interface.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int RegisterAudioSessionNotification(
            [In] IAudioSessionEvents client);

        /// <summary>
        /// Deletes a previous registration by the client to receive notifications.
        /// </summary>
        /// <param name="client">A client-implemented <see cref="IAudioSessionEvents"/> interface.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        new int UnregisterAudioSessionNotification(
            [In] IAudioSessionEvents client);
    /// <summary>
        /// Retrieves the identifier for the audio session.
        /// </summary>
        /// <param name="retVal">Receives the session identifier.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetSessionIdentifier(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string retVal);

        /// <summary>
        /// Retrieves the identifier of the audio session instance.
        /// </summary>
        /// <param name="retVal">Receives the identifier of a particular instance.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetSessionInstanceIdentifier(
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string retVal);

        /// <summary>
        /// Retrieves the process identifier of the audio session.
        /// </summary>
        /// <param name="retVal">Receives the process identifier of the audio session.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int GetProcessId(
            [Out] out UInt32 retVal);

        /// <summary>
        /// Indicates whether the session is a system sounds session.
        /// </summary>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int IsSystemSoundsSession();

        /// <summary>
        /// Enables or disables the default stream attenuation experience (auto-ducking) provided by the system.
        /// </summary>
        /// <param name="optOut">A variable that enables or disables system auto-ducking.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }
}
