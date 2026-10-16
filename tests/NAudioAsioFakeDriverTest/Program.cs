using System;
using NAudioAsioFakeDriverTest;

// RuntimeFeature.IsDynamicCodeSupported isn't usable here: PublishAot turns that switch off even
// under the JIT. A Native AOT publish has no managed assembly beside the executable.
var runtime = System.IO.File.Exists(System.IO.Path.Combine(AppContext.BaseDirectory, "NAudioAsioFakeDriverTest.dll")) ? "JIT" : "Native AOT";
Console.WriteLine($"NAudio.Asio fake-driver test ({runtime}, {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture})");
int failures = FakeAsioDriverChecks.Run(Console.WriteLine);
Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
return failures == 0 ? 0 : 1;
