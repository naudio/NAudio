using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Assert.AreEqual("Hello", ByteEncoding.Instance.GetString(b));
        }

        [Test]
        public void CanTruncate()
        {
            var b = new byte[] {(byte) 'H', (byte) 'e', (byte) 'l', (byte) 'l', (byte) 'o', 0};
            Assert.AreEqual("Hello", ByteEncoding.Instance.GetString(b));
        }

        [Test]
        public void CanTruncateWithThreeParamOverride()
        {
            var b = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0 };
            Assert.AreEqual("Hello", ByteEncoding.Instance.GetString(b,0,b.Length));
        }
    }
}
