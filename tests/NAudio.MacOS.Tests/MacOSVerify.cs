
using System;
using NUnit.Framework;

namespace NAudio.MacOS.Tests;

public static class MacOSVerify
{
    public static void VerifyIsOSMacOSFloorAtLeast(int major = 10, int minor = 2, int build = 0)
    {
        if (!OperatingSystem.IsMacOSVersionAtLeast(major, minor, build))
        {
            Assert.Ignore($"This test requires macOS {major}.{minor}.{build} at least.");
        }
    }
}