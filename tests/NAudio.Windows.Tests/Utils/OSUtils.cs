using System;
using NUnit.Framework;

namespace NAudio.Windows.Tests.Utils;

internal static class OSUtils
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
