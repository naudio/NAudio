
using NAudio.MacOS.CoreAudio;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
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

    private void PrintDevice(AudioDevice dev)
    {
        System.Console.WriteLine($"============== Device {dev.Name} Info ===============");
        System.Console.WriteLine($"Device UID: {dev.DeviceUID}");
        System.Console.WriteLine($"Model UID: {dev.ModelUID}");
        System.Console.WriteLine($"Model Name: {dev.ModelName}");
        System.Console.WriteLine($"Manufacturer: {dev.Manufacturer}");
        System.Console.WriteLine($"Clock Domain: {dev.ClockDomain}");
        System.Console.WriteLine($"Clock Device: {dev.ClockDevice}");
        System.Console.WriteLine($"Configuration Application: {dev.ConfigurationApplication}");
        System.Console.WriteLine($"Is alive: {dev.IsAlive}");
        System.Console.WriteLine($"Is hidden: {dev.IsHidden}");
        System.Console.WriteLine($"Is running: {dev.IsRunning}");
        System.Console.WriteLine($"Is running somewhere: {dev.IsRunningSomewhere}");
        System.Console.WriteLine($"I/O cycle usage: {dev.IOCycleUsage}");
        System.Console.WriteLine($"Actual sample rate: {dev.ActualSampleRate}");
        System.Console.WriteLine($"Buffer frame size: {dev.BufferFrameSize}");
        System.Console.WriteLine($"Buffer frame size range: {dev.BufferFrameSizeRange}");
        System.Console.WriteLine($"Can be default system device: {dev.CanBeDefaultSystemDevice}");
        System.Console.WriteLine($"Icon URI: {dev.Icon}");
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