// build 1 29 Oct 2006
// build 1 is experimental
// updated for Groove Monkee loops so it works on all incoming files
// build 2 30 Oct 2006
// setting to leave MIDI file type unchanged
// settings to force notes to a specific channel
// added clip naming option
// uses new generic about box
// uses new generic progress log
// decoupled logic from Main Form UI
// option not to rename files at all
// Hidden option to trim text markers and remove blank
// Hidden option to insert a name marker and delete all others
// Hidden option to recreate end track markers
// basic install script
// should now be able to cope with multi-track type 1s as input as well
// show hourglass
// remove empty tracks option added
// settings upgrade option
// Update help file
// build 3
// fixed a bug removing blank tracks
// build 4
// advanced options dialog
// clear log on start
// build 5 31 Oct 2006
// Remove extra tempo events option
// Remove extra markers option
// build 6 31 Oct 2006
// option to save conversion log
// build 7 2 Nov 2006
// final build for release
// build 8 3 Nov 2006
// minor changes
// build 9 6 Mar 2007
// renamed to MIDI file converter
// now hosted on CodePlex
// fixed a bug where track 1 didn't have an end track marker if Recreate Track End Markers wasn't set,
// and converting from type 0 to type 1
// build 10 5 Apr 2007
// updated to use new MidiEventCollection
// build 11 12 Feb 2016
// ensuring end track markers persent converting type 1 to 1

### 0.4.0 29 Apr 2022
- updated to latest .NET 6

// revamp help for advanced options

// Next version
// support changing note length
// perhaps allow markers less than final note event
// work out times in measures and beats
// review error handling 
// Consider a command line interface
// Selecting what to copy & what to process (somehow)
// Protect against output folder being a subfolder of input folder

// Testing
// Public release