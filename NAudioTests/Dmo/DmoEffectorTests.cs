using NAudio.Dmo;
using NAudio.Dmo.Effect;
using NAudioTests.Utils;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using NAudio.Wave;

namespace NAudioTests.Dmo
{
    [TestFixture]
    public class DmoEffectorTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoGargle()
        {
            var guid = new Guid("DAFD8210-5711-4B91-9FE3-F75B7AE279BF");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoGargle())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoChorus()
        {
            var guid = new Guid("EFE6629C-81F7-4281-BD91-C9D604A95AF6");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoChorus())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoFlanger()
        {
            var guid = new Guid("EFCA3D92-DFD8-4672-A603-7420894BAD98");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoFlanger())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoEcho()
        {
            var guid = new Guid("EF3E932C-D40B-4F51-8CCF-3F98F1B29D5D");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoEcho())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoDistortion()
        {
            var guid = new Guid("EF114C90-CD1D-484E-96E5-09CFAF912A21");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoDistortion())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoCompressor()
        {
            var guid = new Guid("EF011F79-4000-406D-87AF-BFFB3FC39D57");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoCompressor())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoParamEq()
        {
            var guid = new Guid("120CED89-3BF4-4173-A132-3CB406CF3231");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoParamEq())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoI3DL2Reverb()
        {
            var guid = new Guid("EF985E71-D5C7-42D4-BA4D-2D073E2E96F4");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoI3DL2Reverb())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoWavesReverb()
        {
            var guid = new Guid("87FC0268-9A55-4360-95AA-004A1D9DE26C");
            var targetDescriptor = DmoEnumerator.GetAudioEffectNames().First(descriptor =>
                Equals(descriptor.Clsid, guid));

            using (var dmoGargle = new DmoWavesReverb())
            {
                if (targetDescriptor == null)
                {
                    // is not support
                    Assert.IsNull((object) dmoGargle.MediaObject);
                    Assert.IsNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNull((object) dmoGargle.EffectParams);
                }
                else
                {
                    Assert.IsNotNull((object) dmoGargle.MediaObject);
                    Assert.IsNotNull((object) dmoGargle.MediaObjectInPlace);
                    Assert.IsNotNull((object) dmoGargle.EffectParams);
                }
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanCreateDmoEffectWaveProvider()
        {
            try
            {
                //using (var reader = new Mp3FileReader("C:\\Users\\Wind\\Source\\Repos\\source.mp3"))
                using (WaveStream reader = new NullWaveStream(new WaveFormat(44100, 16, 1), 1000))
                {
                    using (var effector = new DmoEffectWaveProvider<DmoFlanger, DmoFlanger.Params>(reader))
                    {
                        Assert.IsNotNull((object) effector.EffectParams);
                    }
                }
            }
            catch (NotSupportedException)
            {
                Debug.WriteLine("No support Dmo Effect type");
            }
        }

        [Test]
        [Category("IntegrationTest")]
        public void CanReadABlockFromDmoEffectWaveProvider()
        {
            //using (var reader = new Mp3FileReader("C:\\Users\\Wind\\Source\\Repos\\source.mp3"))
            WaveFormat inputFormat = new WaveFormat(44100, 16, 1);
            using (WaveStream reader = new NullWaveStream(inputFormat, inputFormat.AverageBytesPerSecond * 20))
            {
                using (var effector = new DmoEffectWaveProvider<DmoFlanger, DmoFlanger.Params>(reader))
                {
                    // try to read 10 ms;
                    int bytesToRead = effector.WaveFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];
                    int count = effector.Read(buffer, 0, bytesToRead);
                    Assert.That(count > 0, "Bytes Read");
                }
            }
        }

/*
        [Test]
        [Category("IntegrationTest")]
        public void CanCreateEffectedDataFromDmoEffectWaveProvider()
        {
            using (var reader = new Mp3FileReader("C:\\Users\\Wind\\Source\\Repos\\source.mp3"))
            {
                OutPutEffectedWaveFile<DmoGargle, DmoGargle.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_gargle.wav");
                OutPutEffectedWaveFile<DmoChorus, DmoChorus.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_chorus.wav");
                OutPutEffectedWaveFile<DmoFlanger, DmoFlanger.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_flanger.wav");
                OutPutEffectedWaveFile<DmoEcho, DmoEcho.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_echo.wav");
                OutPutEffectedWaveFile<DmoDistortion, DmoDistortion.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_distortion.wav");
                OutPutEffectedWaveFile<DmoCompressor, DmoCompressor.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_compressor.wav");
                OutPutEffectedWaveFile<DmoParamEq, DmoParamEq.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_paramEq.wav");
                OutPutEffectedWaveFile<DmoI3DL2Reverb, DmoI3DL2Reverb.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_i3dl2.wav");
                OutPutEffectedWaveFile<DmoWavesReverb, DmoWavesReverb.Params>(reader, "C:\\Users\\Wind\\Source\\Repos\\dest_reverb.wav");
            }
        }

        public void OutPutEffectedWaveFile<TEffect, TParam>(WaveStream provider, string outputFilePath) where TEffect : IDmoEffector<TParam>, new()
        {
            using (var effector = new DmoEffectWaveProvider<TEffect, TParam>(provider))
            {
                var effectParam = effector.EffectParams;

                using (var writer = new WaveFileWriter(outputFilePath, effector.WaveFormat))
                {
                    // try to read 10 ms;
                    int bytesToRead = effector.WaveFormat.AverageBytesPerSecond / 100;
                    byte[] buffer = new byte[bytesToRead];
                    int count;
                    int total = 0;
                    do
                    {
                        count = effector.Read(buffer, 0, bytesToRead);
                        writer.Write(buffer, 0, count);
                        total += count;
                    } while (count > 0);
                }
            }

            provider.Position = 0;
        }
*/
    }
}