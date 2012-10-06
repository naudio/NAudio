using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NAudioTests.Utils
{
    static class OSUtils
    {
        public static void RequireVista()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                Assert.Ignore("This test requires Windows Vista or newer");
            }
        }

        public static void RequireXP()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                Assert.Ignore("This test requires Windows XP");
            }
        }
    }
}
