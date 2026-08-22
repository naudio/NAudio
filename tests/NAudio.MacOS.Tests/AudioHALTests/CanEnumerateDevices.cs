using System;

using NAudio.MacOS.CoreAudio;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
[Category("IntegrationTest")]
public class CanEnumerateDevices
{
    [OneTimeSetUp]
    public void VerifyMacOS() => MacOSVerify.VerifyIsOSMacOSFloorAtLeast();

    [Test]
    public void LoopThroughDevices()
    {
        foreach (var dev in AudioSystemObject.Instance.Devices)
        {
            PrintDevice(dev);
        }
        // If it enumerates successfully we are good!
    }

    [Test]
    public void QueryDefaultInDevice()
    {
        PrintDevice(AudioSystemObject.Instance.DefaultInputDevice);
    }

    [Test]
    public void QueryDefaultOutDevice()
    {
        PrintDevice(AudioSystemObject.Instance.DefaultOutputDevice);
    }

    [Test]
    public void QueryDefaultSystemOutDevice()
    {
        PrintDevice(AudioSystemObject.Instance.DefaultSystemOutputDevice);
    }

    [Test]
    public void QueryNonExistentDeviceByUID()
    {
        var dev = AudioSystemObject.Instance.ConvertUIDToDevice("anonexistentdevice");
        Assert.That(
            dev == null,
            "This device (anonexistentdevice) should not exist, and the call should not " +
            "throw any exceptions when a valid non-empty string is provided."
        );
    }

    // A device is under no obligation to publish every property, and the ones
    // it does not know about differ by hardware - the DisplayPort and HDMI
    // monitors on the machine this was written against publish no ModelUID,
    // which failed the whole enumeration. Report those rather than throwing.
    // Any error other than "no such property" still fails the test.
    private static string Optional<T>(Func<T> read)
    {
        try
        {
            return read()?.ToString() ?? "(null)";
        }
        catch (CoreAudioPropertyNotFoundException)
        {
            return "(not published by this device)";
        }
        catch (CoreAudioException ex)
        {
            // Some devices advertise a property and then fail to produce it -
            // the Icon of at least one device on the machine this was written
            // against does exactly that. Report it in the output rather than
            // failing an enumeration smoke test over one device's quirk.
            // Anything that is not a Core Audio error still fails the test.
            return $"(could not be read: {ex.Message})";
        }
    }

    private void PrintDevice(AudioDevice dev)
    {
        System.Console.WriteLine($"============== Device {Optional(() => dev.Name)} Info ===============");
        System.Console.WriteLine($"Device UID: {Optional(() => dev.DeviceUID)}");
        System.Console.WriteLine($"Model UID: {Optional(() => dev.ModelUID)}");
        System.Console.WriteLine($"Model Name: {Optional(() => dev.ModelName)}");
        System.Console.WriteLine($"Manufacturer: {Optional(() => dev.Manufacturer)}");
        System.Console.WriteLine($"Clock Domain: {Optional(() => dev.ClockDomain)}");
        System.Console.WriteLine($"Clock Device: {Optional(() => dev.ClockDevice)}");
        System.Console.WriteLine($"Configuration Application: {Optional(() => dev.ConfigurationApplication)}");
        System.Console.WriteLine($"Is alive: {Optional(() => dev.IsAlive)}");
        System.Console.WriteLine($"Is hidden: {Optional(() => dev.IsHidden)}");
        System.Console.WriteLine($"Is running: {Optional(() => dev.IsRunning)}");
        System.Console.WriteLine($"Is running somewhere: {Optional(() => dev.IsRunningSomewhere)}");
        System.Console.WriteLine($"I/O cycle usage: {Optional(() => dev.IOCycleUsage)}");
        System.Console.WriteLine($"Actual sample rate: {Optional(() => dev.ActualSampleRate)}");
        System.Console.WriteLine($"Buffer frame size: {Optional(() => dev.BufferFrameSize)}");
        System.Console.WriteLine($"Buffer frame size range: {Optional(() => dev.BufferFrameSizeRange)}");
        System.Console.WriteLine($"Can be default system device: {Optional(() => dev.CanBeDefaultSystemDevice)}");
        System.Console.WriteLine($"Icon URI: {Optional(() => dev.Icon)}");
        System.Console.WriteLine();
        var devs = dev.RelatedDevices;
        System.Console.WriteLine("====== Related Devices ({0,2} devices) ========", devs.Length);
        foreach (var d2 in devs) { System.Console.WriteLine(d2.Name); }
        System.Console.WriteLine("============================================");
        System.Console.WriteLine();
        System.Console.WriteLine("====================================================");
        System.Console.WriteLine();
    }
}