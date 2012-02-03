using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Audio File Inspector")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Mark Heath")]
[assembly: AssemblyProduct("Audio File Inspector")]
[assembly: AssemblyCopyright("Copyright © Mark Heath 2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8bb855c5-ddfc-4596-a5df-aa9059be7705")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("0.1.7.0")]
[assembly: AssemblyFileVersion("0.1.7.0")]

// build 1 - 1 Nov 2006
// initial version - moved out of WavePlayer
// extracted file describing logic into classes
// drag and drop support
// command line support
// explorer context menu support
// save analysis to text file
// installer
// command line register and unregister file associations
// build 2 - 2 Nov 2006
// fixed some installer problems
// build 3 - 10 Nov 2006
// MIDI note off events are not displayed
// Initial M:B:T support for MIDI
// build 4 - 26 Apr 2007
// Updated to work with latest code in CodePlex
// Beginnings of a find feature
// build 5 - 8 Jun 2008
// Shows length of Wave file as a TimeSpan as well as bytes
// build 6 - 13 Jan 2009
// can report on strc chunks in ACID wav files
// build 7 - 13 Jan 2012
// better MBT calculation for MIDI files

// TODO list
// help file

// better error handling
// possibly: a plugin format
// options for each inspector
// describe on a separate thread


// Enhance existing:
//      MIDI: riff, M:B:T
//      WAV: ACID format
// Additional formats:
//      WAV 64
//      AIFF
//      REX
//      MP3
//      OGG
//      Project 5 pattern
