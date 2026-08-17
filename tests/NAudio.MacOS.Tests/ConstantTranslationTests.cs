
using System;

using NAudio.Utils;

using NUnit.Framework;

namespace NAudio.MacOS.Tests;

// Tests that verify the four char conversion algorithms reliability.
// In both cases, the constants that are used and converted are known
// constants as well as their four char code counterparts (from which
// they are constructed from). These conversions are used in
// many properties of the native macOS API that is wrapped.
[TestFixture]
public class ConstantTranslationTests
{
    [Test]
    public void VerifyThatConstantToValueTranslatesCorrectly()
    {
        uint expectedValue = BitConverter.IsLittleEndian ? 1819304813u : 1835233388u;

        uint retrievedValue = MacUtils.ConstructUIntConstantValueFromString("lpcm");

        Assert.That(
            retrievedValue,
            Is.EqualTo(expectedValue),
            "The constructed constant value must match!"
        );
    }

    [Test]
    public void VerifyThatValueToConstantTranslatesCorrectly()
    {
        string expectedValue = "lpcm";

        string retrievedValue = MacUtils.GetCharCodeFromUIntConstantValue(
            BitConverter.IsLittleEndian ? 1819304813u : 1835233388u
        );

        Assert.That(
            retrievedValue,
            Is.EqualTo(expectedValue),
            "The constructed constant value must match!"
        );
    }

    [Test]
    public void VerifyThatConstantToValueTranslatesCorrectly_Int()
    {
        int expectedValue = BitConverter.IsLittleEndian ? 561017960 : 1752461345;

        int retrievedValue = MacUtils.ConstructIntConstantValueFromString("!pth");

        Assert.That(
            retrievedValue,
            Is.EqualTo(expectedValue),
            "The constructed constant value must match!"
        );
    }

    [Test]
    public void VerifyThatValueToConstantTranslatesCorrectly_Int()
    {
        string expectedValue = "!pth";

        string retrievedValue = MacUtils.GetCharCodeFromIntConstantValue(
            BitConverter.IsLittleEndian ? 561017960 : 1752461345
        );

        Assert.That(
            retrievedValue,
            Is.EqualTo(expectedValue),
            "The constructed constant value must match!"
        );
    }
}