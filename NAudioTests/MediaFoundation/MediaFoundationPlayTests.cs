using NUnit.Framework;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NAudio.MediaFoundation
{
    [TestFixture()]
    public class MediaFoundationPlayTests
    {
        [Test()]
        public void CanPlay()
        {
            string url = @"G:\Chevy\Music\Unravel.mp3";
            if (!File.Exists(url)) Assert.Ignore("Missing test file");
            try
            {
                MediaFoundationProvider provider = new MediaFoundationProvider(url);
                MediaFoundationPlayer player = new MediaFoundationPlayer();
                player.Init(provider);
                player.Play();
            }
            catch
            {
                Assert.Fail();
            }
        }
    }
}