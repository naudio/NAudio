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
	/// SoundFont sample modes
	/// </summary>
	public enum SampleMode
	{
        /// <summary>
        /// No loop
        /// </summary>
		NoLoop,
        /// <summary>
        /// Loop Continuously
        /// </summary>
		LoopContinuously,
        /// <summary>
        /// Reserved no loop
        /// </summary>
		ReservedNoLoop,
        /// <summary>
        /// Loop and continue
        /// </summary>
		LoopAndContinue
	}
}
