using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace NAudio.Utils
{
    /// <summary>
    /// Helper class for registering Windows Explorer File associations
    /// </summary>
    public class FileAssociations
    {

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        static extern void SHChangeNotify(HChangeNotifyEventID wEventId,
                                           HChangeNotifyFlags uFlags,
                                           IntPtr dwItem1,
                                           IntPtr dwItem2);

        /// <summary>
        /// Determines if the specified file type is registered
        /// </summary>
        /// <param name="extension">The file extension including the leading period (e.g. ".wav")</param>
        /// <returns>True if it is registered</returns>
        public static bool IsFileTypeRegistered(string extension)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension);
            if(key == null)
                return false;
            key.Close();
            return true;
        }

        /// <summary>
        /// Gets the HKCR key name for the extenstion
        /// </summary>
        /// <param name="extension">The file extension including the leading period (e.g. ".wav")</param>
        /// <returns>The HKCR key name or null if not registered</returns>
        public static string GetFileTypeKey(string extension)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension);
            string fileTypeKey = null;
            if (key != null)
            {
                 fileTypeKey = (string) key.GetValue(null);
                 key.Close();
            }            
            return fileTypeKey;
        }

        /// <summary>
        /// Registers a file type in Windows
        /// </summary>
        /// <param name="extension">Extension include leading '.'</param>
        /// <param name="description">The description of this file type</param>
        /// <param name="iconPath">Null for no icon or e.g c:\windows\regedit.exe,0</param>
        public static void RegisterFileType(string extension, string description, string iconPath)
        {
            if (IsFileTypeRegistered(extension))
                throw new ArgumentException(extension + "is already registered");
            
            RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension);
            string fileKey = extension.Substring(1) + "File";
            key.SetValue(null, fileKey);            
            key.Close();
            key = Registry.ClassesRoot.CreateSubKey(fileKey);
            key.SetValue(null, description);
            key.Close();
            if (iconPath != null)
            {
                key = Registry.ClassesRoot.CreateSubKey(fileKey + "\\DefaultIcon");
                key.SetValue(null, iconPath);
                key.Close();
            }
            SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Adds a new explorer context menu action for this file type
        /// Will overwrite any existing action with this key
        /// </summary>
        /// <param name="extension">The file extension (include the leading dot)</param>
        /// <param name="actionKey">A unique key for this action</param>
        /// <param name="actionDescription">What will appear on the context menu</param>
        /// <param name="command">The command to execute</param>
        public static void AddAction(string extension, string actionKey, string actionDescription, string command)
        {
            // command e.g. notepad.exe "%1"
            string fileTypeKey = GetFileTypeKey(extension);
            if (fileTypeKey == null)
                throw new ArgumentException(extension + "is not a registered file type");
            RegistryKey key = Registry.ClassesRoot.CreateSubKey(fileTypeKey + "\\shell\\" + actionKey);
            key.SetValue(null, actionDescription);
            key.Close();
            
            key = Registry.ClassesRoot.CreateSubKey(fileTypeKey + "\\shell\\" + actionKey + "\\command");
            key.SetValue(null, command);
            key.Close();
            SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);            
        }

        /// <summary>
        /// Removes an explorer context menu action from a file extension
        /// </summary>
        /// <param name="extension">The file extension (include the leading dot)</param>
        /// <param name="actionKey">The unique key used to register this action</param>
        public static void RemoveAction(string extension, string actionKey)
        {
            string fileTypeKey = GetFileTypeKey(extension);
            if (fileTypeKey == null)
            {
                return;
                //throw new ArgumentException(extension + "is not a registered file type");
            }
            Registry.ClassesRoot.DeleteSubKey(fileTypeKey + "\\shell\\" + actionKey + "\\command");
            Registry.ClassesRoot.DeleteSubKey(fileTypeKey + "\\shell\\" + actionKey);
        }

        // TODO: add ourselves as an "Open With" application
        // TODO: better error handling
        // TODO: unregistering stuff
        // TODO: set default action
        
    }

    #region enum HChangeNotifyEventID
    /// <summary>
    /// Describes the event that has occurred.
    /// Typically, only one event is specified at a time.
    /// If more than one event is specified, the values contained
    /// in the <i>dwItem1</i> and <i>dwItem2</i>
    /// parameters must be the same, respectively, for all specified events.
    /// This parameter can be one or more of the following values.
    /// </summary>
    /// <remarks>
    /// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index
    /// in the system image list that has changed.
    /// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
    /// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index
    /// in the system image list that has changed.
    /// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
    /// </remarks>
    [Flags]
    enum HChangeNotifyEventID
    {
        /// <summary>
        /// All events have occurred.
        /// </summary>
        SHCNE_ALLEVENTS = 0x7FFFFFFF,

        /// <summary>
        /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// must be specified in the <i>uFlags</i> parameter.
        /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>.
        /// </summary>
        SHCNE_ASSOCCHANGED = 0x08000000,

        /// <summary>
        /// The attributes of an item or folder have changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item or folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_ATTRIBUTES = 0x00000800,

        /// <summary>
        /// A nonfolder item has been created.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_CREATE = 0x00000002,

        /// <summary>
        /// A nonfolder item has been deleted.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the item that was deleted.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DELETE = 0x00000004,

        /// <summary>
        /// A drive has been added.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADD = 0x00000100,

        /// <summary>
        /// A drive has been added and the Shell should create a new window for the drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was added.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEADDGUI = 0x00010000,

        /// <summary>
        /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_DRIVEREMOVED = 0x00000080,

        /// <summary>
        /// Not currently used.
        /// </summary>
        SHCNE_EXTENDED_EVENT = 0x04000000,

        /// <summary>
        /// The amount of free space on a drive has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_FREESPACE = 0x00040000,

        /// <summary>
        /// Storage media has been inserted into a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive that contains the new media.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAINSERTED = 0x00000020,

        /// <summary>
        /// Storage media has been removed from a drive.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the root of the drive from which the media was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MEDIAREMOVED = 0x00000040,

        /// <summary>
        /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
        /// or <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was created.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_MKDIR = 0x00000008,

        /// <summary>
        /// A folder on the local computer is being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETSHARE = 0x00000200,

        /// <summary>
        /// A folder on the local computer is no longer being shared via the network.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that is no longer being shared.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_NETUNSHARE = 0x00000400,

        /// <summary>
        /// The name of a folder has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder.
        /// <i>dwItem2</i> contains the new PIDL or name of the folder.
        /// </summary>
        SHCNE_RENAMEFOLDER = 0x00020000,

        /// <summary>
        /// The name of a nonfolder item has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the previous PIDL or name of the item.
        /// <i>dwItem2</i> contains the new PIDL or name of the item.
        /// </summary>
        SHCNE_RENAMEITEM = 0x00000001,

        /// <summary>
        /// A folder has been removed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that was removed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_RMDIR = 0x00000010,

        /// <summary>
        /// The computer has disconnected from a server.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the server from which the computer was disconnected.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// </summary>
        SHCNE_SERVERDISCONNECT = 0x00004000,

        /// <summary>
        /// The contents of an existing folder have changed,
        /// but the folder still exists and has not been renamed.
        /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
        /// <see cref="HChangeNotifyFlags.SHCNF_PATHA"/> must be specified in <i>uFlags</i>.
        /// <i>dwItem1</i> contains the folder that has changed.
        /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
        /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or
        /// SHCNE_RENAMEFOLDER, respectively, instead.
        /// </summary>
        SHCNE_UPDATEDIR = 0x00001000,

        /// <summary>
        /// An image in the system image list has changed.
        /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>.
        /// </summary>
        SHCNE_UPDATEIMAGE = 0x00008000,

    }
    #endregion // enum HChangeNotifyEventID

    #region public enum HChangeNotifyFlags
    /// <summary>
    /// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters.
    /// The uFlags parameter must be one of the following values.
    /// </summary>
    [Flags]
    public enum HChangeNotifyFlags
    {
        /// <summary>
        /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values.
        /// </summary>
        SHCNF_DWORD = 0x0003,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that
        /// represent the item(s) affected by the change.
        /// Each ITEMIDLIST must be relative to the desktop folder.
        /// </summary>
        SHCNF_IDLIST = 0x0000,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHA = 0x0001,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
        /// maximum length MAX_PATH that contain the full path names
        /// of the items affected by the change.
        /// </summary>
        SHCNF_PATHW = 0x0005,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERA = 0x0002,
        /// <summary>
        /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
        /// represent the friendly names of the printer(s) affected by the change.
        /// </summary>
        SHCNF_PRINTERW = 0x0006,
        /// <summary>
        /// The function should not return until the notification
        /// has been delivered to all affected components.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSH = 0x1000,
        /// <summary>
        /// The function should begin delivering notifications to all affected components
        /// but should return as soon as the notification process has begun.
        /// As this flag modifies other data-type flags, it cannot by used by itself.
        /// </summary>
        SHCNF_FLUSHNOWAIT = 0x2000
    }
    #endregion // enum HChangeNotifyFlags

}
