using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.SoundFont 
{
	/// <summary>
	/// A soundfont info chunk
	/// </summary>
	public class InfoChunk 
	{
	    internal InfoChunk(RiffChunk chunk) 
		{
			bool ifilPresent = false;
			bool inamPresent = false;
			if(chunk.ReadChunkID() != "INFO") 
			{
				throw new InvalidDataException("Not an INFO chunk");
			}
			//this.chunk = chunk;
			RiffChunk c;
			while((c = chunk.GetNextSubChunk()) != null) 
			{
				switch(c.ChunkID) 
				{
				case "ifil":
					ifilPresent = true;
					SoundFontVersion = c.GetDataAsStructure(new SFVersionBuilder());
					break;
				case "isng":
					WaveTableSoundEngine = c.GetDataAsString();
					break;
				case "INAM":
					inamPresent = true;
					BankName = c.GetDataAsString();
					break;
				case "irom":
					DataROM = c.GetDataAsString();
					break;
				case "iver":
					ROMVersion = c.GetDataAsStructure(new SFVersionBuilder());
					break;
				case "ICRD":
					CreationDate = c.GetDataAsString();
					break;
				case "IENG":
					Author = c.GetDataAsString();
					break;
				case "IPRD":
					TargetProduct = c.GetDataAsString();
					break;
				case "ICOP":
					Copyright = c.GetDataAsString();
					break;
				case "ICMT":
					Comments = c.GetDataAsString();
					break;
				case "ISFT":
					Tools = c.GetDataAsString();
					break;
				default:
					throw new InvalidDataException($"Unknown chunk type {c.ChunkID}");
				}
			}
			if(!ifilPresent) 
			{
                throw new InvalidDataException("Missing SoundFont version information");
			}
            // n.b. issue #150 - it is valid for isng not to be present
			if(!inamPresent) 
			{
                throw new InvalidDataException("Missing SoundFont name information");
			}
		}

		/// <summary>
		/// SoundFont Version
		/// </summary>
		public SFVersion SoundFontVersion { get; }

	    /// <summary>
		/// WaveTable sound engine
		/// </summary>
		public string WaveTableSoundEngine { get; set; }

	    /// <summary>
		/// Bank name
		/// </summary>
		public string BankName { get; set; }

	    /// <summary>
		/// Data ROM
		/// </summary>
	    // ReSharper disable once InconsistentNaming
		public string DataROM { get; set; }

	    /// <summary>
		/// Creation Date
		/// </summary>
		public string CreationDate { get; set; }

	    /// <summary>
		/// Author
		/// </summary>
		public string Author { get; set; }

	    /// <summary>
		/// Target Product
		/// </summary>
		public string TargetProduct { get; set; }

	    /// <summary>
		/// Copyright
		/// </summary>
		public string Copyright { get; set; }

	    /// <summary>
		/// Comments
		/// </summary>
		public string Comments { get; set; }

	    /// <summary>
		/// Tools
		/// </summary>
		public string Tools { get; set; }

	    /// <summary>
		/// ROM Version
		/// </summary>
	    // ReSharper disable once InconsistentNaming
		public SFVersion ROMVersion { get; set; }

	    /// <summary>
		/// <see cref="Object.ToString"/>
		/// </summary>
		public override string ToString() 
		{
			return string.Format("Bank Name: {0}\r\nAuthor: {1}\r\nCopyright: {2}\r\nCreation Date: {3}\r\nTools: {4}\r\nComments: {5}\r\nSound Engine: {6}\r\nSoundFont Version: {7}\r\nTarget Product: {8}\r\nData ROM: {9}\r\nROM Version: {10}",
				BankName,
				Author,
				Copyright,
				CreationDate,
				Tools,
				"TODO-fix comments",//Comments,
				WaveTableSoundEngine,
				SoundFontVersion,
				TargetProduct,
				DataROM,
				ROMVersion);
		}
	}
}