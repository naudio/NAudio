// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
using System;

namespace NAudio.SoundFont 
{
	class SampleDataChunk 
	{
		private byte[] sampleData;
		public SampleDataChunk(RiffChunk chunk) 
		{
			string header = chunk.ReadChunkID();
			if(header != "sdta") 
			{
				throw new ApplicationException(String.Format("Not a sample data chunk ({0})",header));
			}
			sampleData = chunk.GetData();
		}

		public byte[] SampleData
		{
			get
			{
				return sampleData;
			}
		}
	}

} // end of namespace