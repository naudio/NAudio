using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudioDemo.VolumeMixerDemo
{
    public class IconExtractor
    {
        public static Icon Extract(string file, int number, bool largeIcon)
        {
            if (ExtractIconEx(file, number, out IntPtr large, out IntPtr small, 1) <= 0)
                return null;
            try
            {
                var wanted = largeIcon ? large : small;
                var unused = largeIcon ? small : large;
                if (unused != IntPtr.Zero) DestroyIcon(unused);
                return wanted == IntPtr.Zero ? null : Icon.FromHandle(wanted);
            }
            catch
            {
                return null;
            }
        }

        [DllImport("Shell32.dll")]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        [DllImport("User32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
