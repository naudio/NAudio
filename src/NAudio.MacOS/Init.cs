
using System;
using System.Runtime.CompilerServices;

internal static class ModuleInitialization
{
    [ModuleInitializer]
    public static void OnInit()
    {
        if (!OperatingSystem.IsMacOSVersionAtLeast(10, 2))
        {
            throw new PlatformNotSupportedException(
                "macOS wrappers are not supported in this version: " + Environment.OSVersion.VersionString
            );
        }
    }
}