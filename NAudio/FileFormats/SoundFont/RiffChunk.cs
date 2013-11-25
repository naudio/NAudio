using System;
using System.IO;
using System.Net;
using System.Text;
using NAudio.Utils;

namespace NAudio.SoundFont 
{
	internal class RiffChunk 
	{
		private string chunkID;
		private uint chunkSize;
		private long dataOffset; // data offset in the file
		private BinaryReader riffFile;
		
		public static RiffChunk GetTopLevelChunk(BinaryReader file) 
		{
			RiffChunk r = new RiffChunk(file);
			r.ReadChunk();
			return r;
		}
		
		private RiffChunk(BinaryReader file) 
		{
			riffFile = file;
			chunkID = "????";
			chunkSize = 0;
			dataOffset = 0;
		}

		/// <summary>
		/// just reads a chunk ID at the current position
		/// </summary>
		/// <returns>chunk ID</returns>
		public string ReadChunkID() 
		{
			byte []cid = riffFile.ReadBytes(4);
			if(cid.Length != 4) 
			{
				throw new InvalidDataException("Couldn't read Chunk ID");
			}
			return ByteEncoding.Instance.GetString(cid, 0, cid.Length);
		}
		
		/// <summary>
		/// reads a chunk at the current position
		/// </summary>
 		private void ReadChunk() 
		{
			this.chunkID = ReadChunkID();
			this.chunkSize = riffFile.ReadUInt32(); //(uint) IPAddress.NetworkToHostOrder(riffFile.ReadUInt32());
			this.dataOffset = riffFile.BaseStream.Position;
		}
		
		/// <summary>
		/// creates a new riffchunk from current position checking that we're not
		/// at the end of this chunk first
		/// </summary>
		/// <returns>the new chunk</returns>
		public RiffChunk GetNextSubChunk() 
		{
			if(riffFile.BaseStream.Position + 8 < dataOffset + chunkSize) 
			{
				RiffChunk chunk = new RiffChunk(riffFile);
				chunk.ReadChunk();
				return chunk;
			}
			//Console.WriteLine("DEBUG Failed to GetNextSubChunk because Position is {0}, dataOffset{1}, chunkSize {2}",riffFile.BaseStream.Position,dataOffset,chunkSize);
			return null;
		}
		
		public byte[] GetData() 
		{
			riffFile.BaseStream.Position = dataOffset;
			byte[] data = riffFile.ReadBytes((int) chunkSize);
			if(data.Length != chunkSize) 
			{
				throw new InvalidDataException(String.Format("Couldn't read chunk's data Chunk: {0}, read {1} bytes",this,data.Length));
			}
			return data;
		}
		
		/// <summary>
		/// useful for chunks that just contain a string
		/// </summary>
		/// <returns>chunk as string</returns>
		public string GetDataAsString() 
		{
			byte[] data = GetData();
			if(data == null)
				return null;
			return ByteEncoding.Instance.GetString(data, 0, data.Length);
		}
		
		public T GetDataAsStructure<T>(StructureBuilder<T> s) 
		{
			riffFile.BaseStream.Position = dataOffset;
			if(s.Length != chunkSize) 
			{
				throw new InvalidDataException(String.Format("Chunk size is: {0} so can't read structure of: {1}",chunkSize,s.Length));
			}
			return s.Read(riffFile);
		}
		
		public T[] GetDataAsStructureArray<T>(StructureBuilder<T> s) 
		{
			riffFile.BaseStream.Position = dataOffset;
			if(chunkSize % s.Length != 0) 
			{
                throw new InvalidDataException(String.Format("Chunk size is: {0} not a multiple of structure size: {1}", chunkSize, s.Length));
			}
			int structuresToRead = (int) (chunkSize / s.Length);
			T[] a = new T[structuresToRead];
			for(int n = 0; n < structuresToRead; n++) 
			{
				a[n] = s.Read(riffFile);
			}
			return a;
		}
		
		public string ChunkID 
		{
			get 
			{
				return chunkID;
			}
			set 
			{
				if(value == null) 
				{
					throw new ArgumentNullException("ChunkID may not be null");
				}
				if(value.Length != 4) 
				{
					throw new ArgumentException("ChunkID must be four characters");
				}
				chunkID = value;
			}
		}
		
		public uint ChunkSize 
		{
			get 
			{
				return chunkSize;
			}
		}
		
		public long DataOffset 
		{
			get 
			{
				return dataOffset;
			}
		}
		
		public override string ToString() 
		{
			return String.Format("RiffChunk ID: {0} Size: {1} Data Offset: {2}",ChunkID,ChunkSize,DataOffset);
		}
			
	}

}
