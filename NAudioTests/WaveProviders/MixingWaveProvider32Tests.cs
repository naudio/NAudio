using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveProviders
{
    [TestFixture]
    public class MixingWaveProvider32Tests
    {
        [Test]
        public void CannotAddSameInputProviderSecondTime()

        {
            // arrange
            var sut = new MixingWaveProvider32();
            var bufferedWaveProvider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
            sut.AddInputStream(bufferedWaveProvider);

            // act
            sut.AddInputStream(bufferedWaveProvider);

            // assert
            Assert.AreEqual(sut.InputCount, 1);
        }
    }
}
