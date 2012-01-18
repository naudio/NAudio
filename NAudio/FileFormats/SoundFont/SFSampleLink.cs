// ©Mark Heath 2006 (mark@wordandspirit.co.uk)
// You are free to use this code for your own projects.
// Please consider giving credit somewhere in your app to this code if you use it
// Please do not redistribute this code without my permission
// Please get in touch and let me know of any bugs you find, enhancements you would like,
// and apps you have written
namespace NAudio.SoundFont 
{
	/// <summary>
	/// Sample Link Type
	/// </summary>
	public enum SFSampleLink : ushort 
	{
		/// <summary>
		/// Mono Sample
		/// </summary>
		MonoSample = 1,
		/// <summary>
		/// Right Sample
		/// </summary>
		RightSample = 2,
		/// <summary>
		/// Left Sample
		/// </summary>
		LeftSample = 4,
		/// <summary>
		/// Linked Sample
		/// </summary>
		LinkedSample = 8,
		/// <summary>
		/// ROM Mono Sample
		/// </summary>
		RomMonoSample = 0x8001,
		/// <summary>
		/// ROM Right Sample
		/// </summary>
		RomRightSample = 0x8002,
		/// <summary>
		/// ROM Left Sample
		/// </summary>
		RomLeftSample = 0x8004,
		/// <summary>
		/// ROM Linked Sample
		/// </summary>
		RomLinkedSample = 0x8008
	}
}