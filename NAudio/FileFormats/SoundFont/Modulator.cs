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
	/// Transform Types
	/// </summary>
	public enum TransformEnum 
	{
		/// <summary>
		/// Linear
		/// </summary>
		Linear = 0
	}
	
	/// <summary>
	/// Modulator
	/// </summary>
	public class Modulator 
	{
		private ModulatorType sourceModulationData;
		private GeneratorEnum destinationGenerator;
		private short amount;
		private ModulatorType sourceModulationAmount;
		private TransformEnum sourceTransform;
		
		/// <summary>
		/// Source Modulation data type
		/// </summary>
		public ModulatorType SourceModulationData 
		{
			get 
			{
				return sourceModulationData;
			}
			set 
			{
				sourceModulationData = value;
			}
		}
		
		/// <summary>
		/// Destination generator type
		/// </summary>
		public GeneratorEnum DestinationGenerator 
		{
			get 
			{
				return destinationGenerator;
			}
			set 
			{
				destinationGenerator = value;
			}
		}
		
		/// <summary>
		/// Amount
		/// </summary>
		public short Amount 
		{
			get 
			{
				return amount;
			}
			set 
			{
				amount = value;
			}
		}
		
		/// <summary>
		/// Source Modulation Amount Type
		/// </summary>
		public ModulatorType SourceModulationAmount 
		{
			get 
			{
				return sourceModulationAmount;
			}
			set 
			{
				sourceModulationAmount = value;
			}
		}
		
		/// <summary>
		/// Source Transform Type
		/// </summary>
		public TransformEnum SourceTransform 
		{
			get 
			{
				return sourceTransform;
			}
			set 
			{
				sourceTransform = value;
			}
		}
		
		/// <summary>
		/// <see cref="Object.ToString"/>
		/// </summary>
		public override string ToString()
		{
			return String.Format("Modulator {0} {1} {2} {3} {4}",
				sourceModulationData,destinationGenerator,
				amount,sourceModulationAmount,sourceTransform);

		}

	}
}