
using System;

internal static class VersioningVerifier
{
    // Verifies that we are running the assembly in a supported macOS version.
    // Floor: 10.2, because some of the API's we use were introduced back then.
    // Although .NET does not support 10.2 we do this if .NET actually decides to go that back.
    public static void VerifyWeAreInSupportedVersion()
    {
        if (!OperatingSystem.IsMacOSVersionAtLeast(10, 2))
        {
            throw new PlatformNotSupportedException(
                "macOS wrappers are not supported in this version: " + Environment.OSVersion.VersionString
            );
        }
    }
}