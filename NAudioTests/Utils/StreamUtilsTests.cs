
using System.IO;
using NAudio.Utils;
using NUnit.Framework;

namespace NAudioTests.Utils
{
    [TestFixture]
    [Category("UnitTest")]
    public class StreamUtilsTests
    {
        [Test]
        public void TestLittleEndianMethods()
        {
            using var ms = new MemoryStream();

            StreamUtils.WriteShortLittleEndian(ms, short.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadShortLittleEndian(ms),
                Is.EqualTo(short.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteUShortLittleEndian(ms, ushort.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadUShortLittleEndian(ms),
                Is.EqualTo(ushort.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteIntLittleEndian(ms, int.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadIntLittleEndian(ms),
                Is.EqualTo(int.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteUIntLittleEndian(ms, uint.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadUIntLittleEndian(ms),
                Is.EqualTo(uint.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteLongLittleEndian(ms, long.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadLongLittleEndian(ms),
                Is.EqualTo(long.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteULongLittleEndian(ms, ulong.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadULongLittleEndian(ms),
                Is.EqualTo(ulong.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteFloatLittleEndian(ms, float.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadFloatLittleEndian(ms),
                Is.EqualTo(float.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteDoubleLittleEndian(ms, double.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadDoubleLittleEndian(ms),
                Is.EqualTo(double.MaxValue)
            );
        }

        [Test]
        public void TestBigEndianMethods()
        {
            using var ms = new MemoryStream();

            StreamUtils.WriteShortBigEndian(ms, short.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadShortBigEndian(ms),
                Is.EqualTo(short.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteUShortBigEndian(ms, ushort.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadUShortBigEndian(ms),
                Is.EqualTo(ushort.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteIntBigEndian(ms, int.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadIntBigEndian(ms),
                Is.EqualTo(int.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteUIntBigEndian(ms, uint.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadUIntBigEndian(ms),
                Is.EqualTo(uint.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteLongBigEndian(ms, long.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadLongBigEndian(ms),
                Is.EqualTo(long.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteULongBigEndian(ms, ulong.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadULongBigEndian(ms),
                Is.EqualTo(ulong.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteFloatBigEndian(ms, float.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadFloatBigEndian(ms),
                Is.EqualTo(float.MaxValue)
            );

            ms.SetLength(0);

            StreamUtils.WriteDoubleBigEndian(ms, double.MaxValue);
            ms.Position = 0;
            Assert.That(
                StreamUtils.ReadDoubleBigEndian(ms),
                Is.EqualTo(double.MaxValue)
            );
        }

        [Test]
        public void TestStringMethods()
        {
            using var ms = new MemoryStream();

            string test_string = "Hello from random string";

            long bytes_written = StreamUtils.WriteString(ms, test_string, System.Text.Encoding.UTF8);

            ms.Position = 0;

            Assert.That(
                StreamUtils.ReadString(ms, System.Text.Encoding.UTF8, bytes_written),
                Is.EqualTo(test_string)
            );

            ms.SetLength(0);

            bytes_written = StreamUtils.WriteString(ms, test_string, System.Text.Encoding.UTF32);

            ms.Position = 0;

            Assert.That(
                StreamUtils.ReadString(ms, System.Text.Encoding.UTF32, bytes_written),
                Is.EqualTo(test_string)
            );

            ms.SetLength(0);

            bytes_written = StreamUtils.WriteString(ms, test_string, System.Text.Encoding.BigEndianUnicode);

            ms.Position = 0;

            Assert.That(
                StreamUtils.ReadString(ms, System.Text.Encoding.BigEndianUnicode, bytes_written),
                Is.EqualTo(test_string)
            );
        }
    }
}
