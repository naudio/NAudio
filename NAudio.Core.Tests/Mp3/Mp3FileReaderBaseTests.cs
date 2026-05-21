using System.IO;
using NAudio.Wave;
using NAudio.Core.Tests.Utils;
using NUnit.Framework;

namespace NAudio.Core.Tests.Mp3
{
    [TestFixture]
    public class Mp3FileReaderBaseTests
    {
        [Test]
        [Category("UnitTest")]
        public void DisposesFileOnFailToParse()
        {
            // If File.Delete here fails with a sharing violation, the ctor failed to release
            // the file handle on its parsing-error path (see Mp3FileReaderBase ctor catch block).
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Some test content");
            try
            {
                Assert.Throws<InvalidDataException>(() =>
                    new Mp3FileReaderBase(tempFilePath, fmt => new FakeMp3FrameDecompressor(fmt)));
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        [Test]
        [Category("UnitTest")]
        public void CopesWithZeroLengthStream()
        {
            var ms = new MemoryStream(new byte[0]);
            Assert.Throws<InvalidDataException>(() =>
                new Mp3FileReaderBase(ms, fmt => new FakeMp3FrameDecompressor(fmt)));
        }
    }
}
