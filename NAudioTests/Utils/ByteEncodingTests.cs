using System;
using System.Linq;
using NAudio.Utils;
using NUnit.Framework;

namespace NAudioTests.Utils
{
    [TestFixture]
    public class ByteEncodingTests
    {
        [Test]
        public void CanDecodeString()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', };
            Assert.That(ByteEncoding.Instance.GetString(b), Is.EqualTo("Hello"));
        }

        [Test]
        public void CanTruncate()
        {
            var b = new byte[] {(byte) 'H', (byte) 'e', (byte) 'l', (byte) 'l', (byte) 'o', 0};
            Assert.That(ByteEncoding.Instance.GetString(b), Is.EqualTo("Hello"));
        }

        [Test]
        public void CanTruncateWithThreeParamOverride()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
            Assert.That(ByteEncoding.Instance.GetString(b,0,b.Length), Is.EqualTo("Hello"));
        }
    }
}
