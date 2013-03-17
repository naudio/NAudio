// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
using System;

namespace NAudio.SoundFont 
{
	/// <summary>
	/// Soundfont generator
	/// </summary>
	public class Generator 
	{
		private GeneratorEnum generatorType;
		private ushort rawAmount;
		private Instrument instrument;
		private SampleHeader sampleHeader;
		
		/// <summary>
		/// Gets the generator type
		/// </summary>
		public GeneratorEnum GeneratorType 
		{
			get 
			{
				return generatorType;
			}
			set 
			{
				generatorType = value;
			}
		}
		
		/// <summary>
		/// Generator amount as an unsigned short
		/// </summary>
		public ushort UInt16Amount 
		{
			get 
			{
				return rawAmount;
			}
			set 
			{
				rawAmount = value;
			}
		}
		
		/// <summary>
		/// Generator amount as a signed short
		/// </summary>
		public short Int16Amount 
		{
			get 
			{
				return (short) rawAmount;
			}
			set 
			{
				rawAmount = (ushort) value;
			}
		}
		
		/// <summary>
		/// Low byte amount
		/// </summary>
		public byte LowByteAmount 
		{
			get 
			{
				return (byte) (rawAmount & 0x00FF);
			}
			set 
			{
				rawAmount &= 0xFF00;
				rawAmount += value;
			}
		}
		
		/// <summary>
		/// High byte amount
		/// </summary>
		public byte HighByteAmount 
		{
			get 
			{
				return (byte) ((rawAmount & 0xFF00) >> 8);
			}
			set 
			{
				rawAmount &= 0x00FF;
				rawAmount += (ushort) (value << 8);
			}
		}

		/// <summary>
		/// Instrument
		/// </summary>
		public Instrument Instrument
		{
			get
			{
				return instrument;
			}
			set
			{
				instrument = value;
			}
		}

		/// <summary>
		/// Sample Header
		/// </summary>
		public SampleHeader SampleHeader
		{
			get
			{
				return sampleHeader;
			}
			set
			{
				sampleHeader = value;
			}
		}

		/// <summary>
		/// <see cref="object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			if(generatorType == GeneratorEnum.Instrument)
				return String.Format("Generator Instrument {0}",instrument.Name);
			else if(generatorType == GeneratorEnum.SampleID)
				return String.Format("Generator SampleID {0}",sampleHeader);
			else
				return String.Format("Generator {0} {1}",generatorType,rawAmount);
		}

	}
}