using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Ogg
{
	/// <summary>
	/// Provides access to the unmanaged ogg encoder DLL
	/// </summary>
	public class OggEncoder : IDisposable
	{
		private const int READ = 1024;

		private IntPtr memPool = IntPtr.Zero;
		private int memPos = 0;
		private const int MEM_POOL_SIZE = 2048; 
		
		private IntPtr AllocateHGlobal(int bytes)
		{
			if(memPool == IntPtr.Zero)
			{
				memPool = Marshal.AllocHGlobal(MEM_POOL_SIZE);
				memPos = 0;
			}
			if(memPos + bytes > MEM_POOL_SIZE)
			{
				throw new ApplicationException("Need a bigger memory pool");
			}
			IntPtr ret = new IntPtr(memPool.ToInt32() + memPos);
			memPos += bytes;
			
			for(int n = 0; n < bytes; n++)
			{
				Marshal.WriteByte(ret,n,0);
			}
			GC.KeepAlive(this);
			return ret;
			//return Marshal.AllocHGlobal(bytes);
		}

		private void FreeHGlobal()
		{
			Marshal.FreeHGlobal(memPool);
			memPos = 0;
			memPool = IntPtr.Zero;
		}

		// some buffers comfortably big enough for the various ogg / vorbis structures
		IntPtr os;		// ogg_stream_state take physical pages, weld into a logical stream of packets
		IntPtr op;		// ogg_packet one raw packet of data for decode
		IntPtr vi;		// vorbis_info struct that stores all the static vorbis bitstream settings
		IntPtr vc;		// vorbis_comment struct that stores all the user comments
		IntPtr vd;		// vorbis_dsp_state central working state for the packet->PCM decoder
		IntPtr vb;		// vorbis_block local working space for packet->PCM decode
		//ogg_page og = new ogg_page(); 	// ogg_page one Ogg bitstream page.  Vorbis packets are inside
		IntPtr og;		// ogg_page one Ogg bitstream page.  Vorbis packets are inside

		/// <summary>
		/// Creates a new ogg encoder
		/// </summary>
		public OggEncoder()
		{
			// We must use IntPtrs as the vd structure will contain a
			// pointer to the vi structure, so we can't allow it to move
			// around in memory during a garbage collection
			// we are allocating comfortably more than we need to just for safety
			os = AllocateHGlobal(512);
			vb = AllocateHGlobal(256);
			vd = AllocateHGlobal(256);
			vi = AllocateHGlobal(64);
			op = AllocateHGlobal(64);
			vc = AllocateHGlobal(32);
			og = AllocateHGlobal(32);

		}

		/// <summary>
		/// Finalizer for the ogg encoder
		/// </summary>
		~OggEncoder()
		{
			System.Diagnostics.Debug.Assert(true,"Ogg encoder wasn't disposed");
			Dispose(false);

		}

		/// <summary>
		/// Function to encode a Wave file to OGG
		/// </summary>
		/// <param name="infile">Wave file name</param>
		/// <param name="outfile">Ogg file name</param>
		public void Encode(string infile, string outfile)
		{
			WaveFileReader reader = new WaveFileReader(infile);
			Stream stdout = File.OpenWrite(outfile);


			try
			{
				// Encode setup
				OggInterop.vorbis_info_init(vi);
				
				// choose an encoding mode

				/*********************************************************************
				Encoding using a VBR quality mode.  The usable range is -.1
				(lowest quality, smallest file) to 1. (highest quality, largest file).
				Example quality mode .4: 44kHz stereo coupled, roughly 128kbps VBR 

				ret = vorbis_encode_init_vbr(&vi,2,44100,.4);

				---------------------------------------------------------------------

				Encoding using an average bitrate mode (ABR).
				example: 44kHz stereo coupled, average 128kbps VBR 

				ret = vorbis_encode_init(&vi,2,44100,-1,128000,-1);

				---------------------------------------------------------------------

				Encode using a qulity mode, but select that quality mode by asking for
				an approximate bitrate.  This is not ABR, it is true VBR, but selected
				using the bitrate interface, and then turning bitrate management off:

				ret = ( vorbis_encode_setup_managed(&vi,2,44100,-1,128000,-1) ||
					vorbis_encode_ctl(&vi,OV_ECTL_RATEMANAGE_AVG,NULL) ||
					vorbis_encode_setup_init(&vi));

				*********************************************************************/

				// do not continue if setup failed; this can happen if we ask for a
				// mode that libVorbis does not support (eg, too low a bitrate, etc,
				// will return 'OV_EIMPL')
				if(OggInterop.vorbis_encode_init_vbr(vi,reader.WaveFormat.Channels,reader.WaveFormat.SampleRate,0.5f) != 0)
					throw new ApplicationException("vorbis_encode_init_vbr");
				
				// add a comment
				//vorbis_comment_init(vc);
				//vorbis_comment_add_tag(vc,"ENCODER","OggEncoder.cs");
				//vorbis_comment_add_tag(vc,"ARTIST","Mark Heath");
				//vorbis_comment_add_tag(vc,"TITLE",Path.GetFileNameWithoutExtension(infile));

				// MRH: possibly redundant step, but in the oggtools app
				// seems to let us get past the null reference exception in
				// vorbis_analysis_init the second time through
				// (but then we get stuck on vorbis_info_clear)
				OggInterop.vorbis_encode_setup_init(vi);
				
				// set up the analysis state and auxiliary encoding storage
				if(OggInterop.vorbis_analysis_init(vd,vi) != 0)
					throw new ApplicationException("vorbis_analysis_init error");

				if(OggInterop.vorbis_block_init(vd,vb) != 0)
					throw new ApplicationException("vorbis_block_init error");

				// set up our packet->stream encoder
				// pick a random serial number; that way we can more likely build
				// chained streams just by concatenation
				Random rand = new Random();
				if(OggInterop.ogg_stream_init(os,rand.Next()) != 0)
					throw new ApplicationException("ogg_stream_init error");

				// Vorbis streams begin with three headers; the initial header (with
				// most of the codec setup parameters) which is mandated by the Ogg
				// bitstream spec.  The second header holds any comment fields.  The
				// third header holds the bitstream codebook.  We merely need to
				// make the headers, then pass them to libvorbis one at a time;
				// libvorbis handles the additional Ogg bitstream constraints 

				IntPtr header = AllocateHGlobal(64); //ogg_packet 
				IntPtr header_comments = AllocateHGlobal(64); //ogg_packet 
				IntPtr header_codebook = AllocateHGlobal(64); //ogg_packet 

				OggInterop.vorbis_analysis_headerout(vd,vc,header,header_comments,header_codebook);
				OggInterop.ogg_stream_packetin(os,header); // automatically placed in its own page
				OggInterop.ogg_stream_packetin(os,header_comments);
				OggInterop.ogg_stream_packetin(os,header_codebook);

				// This ensures the actual audio data will start on a new page, as per spec
				while(OggInterop.ogg_stream_flush(os,og) != 0)
				{
					WriteOg(og,stdout);
				}
		  
				float[][] samplebuffer = new float[reader.WaveFormat.Channels][];
				for(int channel = 0; channel < reader.WaveFormat.Channels; channel++)
				{
					samplebuffer[channel] = new float[READ];
				}

				bool eos=false;
				while(!eos)
				{
					int samples = reader.Read(samplebuffer,READ);
					if(samples == 0)
					{
						// end of file.  this can be done implicitly in the mainline,
						//but it's easier to see here in non-clever fashion.
						//Tell the library we're at end of stream so that it can handle
						//the last frame and mark end of stream in the output properly 
						OggInterop.vorbis_analysis_wrote(vd,0);
					}
					else
					{
						// data to encode 
						// expose the buffer to submit data 
						IntPtr bufferpointer = OggInterop.vorbis_analysis_buffer(vd,samples);
						int[] floatpointers = new int[reader.WaveFormat.Channels];
						Marshal.Copy(bufferpointer,floatpointers,0,reader.WaveFormat.Channels);
						for(int channel = 0; channel < reader.WaveFormat.Channels; channel++)
						{
							IntPtr channelbuffer = new IntPtr(floatpointers[channel]);
							Marshal.Copy(samplebuffer[channel],0,channelbuffer,samples);					
						}

						// tell the library how much we actually submitted
						OggInterop.vorbis_analysis_wrote(vd,samples);
					}

					// vorbis does some data preanalysis, then divvies up blocks for
					// more involved (potentially parallel) processing.  Get a single
					// block for encoding now
					while(OggInterop.vorbis_analysis_blockout(vd,vb)==1)
					{

						/* analysis, assume we want to use bitrate management */
						OggInterop.vorbis_analysis(vb,IntPtr.Zero);
						OggInterop.vorbis_bitrate_addblock(vb);

						while(OggInterop.vorbis_bitrate_flushpacket(vd,op) != 0)
						{

							/* weld the packet into the bitstream */
							OggInterop.ogg_stream_packetin(os,op);

							/* write out pages (if any) */
							while(!eos)
							{
								int result=OggInterop.ogg_stream_pageout(os,og);
								if(result==0)
									break;
								WriteOg(og,stdout);

								/* this could be set above, but for illustrative purposes, I do
								it here (to show that vorbis does know where the stream ends) */

								if(OggInterop.ogg_page_eos(og) != 0)
									eos=true;
							}
						}
					}
				}

				// clean up and exit.  vorbis_info_clear() must be called last */

				if(OggInterop.ogg_stream_clear(os) != 0)
					throw new ApplicationException("ogg_stream_clear error");
				if(OggInterop.vorbis_block_clear(vb) != 0)
					throw new ApplicationException("vorbis_block_clear error");
				OggInterop.vorbis_dsp_clear(vd);
				//vorbis_comment_clear(vc);			
				OggInterop.vorbis_info_clear(vi);
				
				
				// ogg_page and ogg_packet structs always point to storage in
				// libvorbis.  They're never freed or manipulated directly
			}
			finally
			{
				reader.Dispose();
				stdout.Close();
			}
			GC.KeepAlive(this);
		}

		/*	private void WriteOg(ogg_page og,Stream stdout)
			{
				byte[] ogheader = new byte[og.header_len];
				Marshal.Copy(og.header,ogheader,0,og.header_len);
				stdout.Write(ogheader,0,og.header_len);
				byte[] ogbody = new byte[og.body_len];				
				Marshal.Copy(og.body,ogbody,0,og.body_len);
				stdout.Write(ogbody,0,og.body_len);
			}
		*/
		private void WriteOg(IntPtr ogptr,Stream stdout)
		{
			ogg_page og = new ogg_page();
			//Marshal.PtrToStructure(ogptr,og);
			og.header = Marshal.ReadIntPtr(ogptr);
			og.header_len = Marshal.ReadInt32(ogptr,4);
			og.body = Marshal.ReadIntPtr(ogptr,8);
			og.body_len = Marshal.ReadInt32(ogptr,12);

			byte[] ogheader = new byte[og.header_len];
			Marshal.Copy(og.header,ogheader,0,og.header_len);
			stdout.Write(ogheader,0,og.header_len);
			byte[] ogbody = new byte[og.body_len];				
			Marshal.Copy(og.body,ogbody,0,og.body_len);
			stdout.Write(ogbody,0,og.body_len);
		}
		#region IDisposable Members

		private void Dispose(bool disposing)
		{
			FreeHGlobal();
		}

		/// <summary>
		/// Closes the encoder and frees any associated memory
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}