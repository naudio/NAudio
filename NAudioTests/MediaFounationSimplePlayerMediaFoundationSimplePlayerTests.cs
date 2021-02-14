using NUnit.Framework;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NAudioTests.MediaFoundation
{
    [TestFixture()]
    public class MediaFounationSimplePlayerMediaFoundationSimplePlayerTests
    {
        [Test]
        public void Play (){
            MediaFounationSimplePlayer player = new MediaFounationSimplePlayer(@"G:\Chevy\Music\Unravel.mp3");
            while (!player.IsPrepared) { }
            player.Rate = 2;
            player.Volume = 1;
            Debug.WriteLine(player.Duration);
            player.Play(0);

        }
    }
}