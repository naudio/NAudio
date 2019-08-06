#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

// TODO: port the ZipLib, ZipDemo and ZipRelease tasks from the old fake scripts

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./NAudio/bin") + Directory(configuration);

var buildLogo = @"  _   _    _             _ _       
 | \ | |  / \  _   _  __| (_) ___  
 |  \| | / _ \| | | |/ _` | |/ _ \ 
 | |\  |/ ___ \ |_| | (_| | | (_) |
 |_| \_/_/   \_\__,_|\__,_|_|\___/ 
"; 
Information(buildLogo);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/bin/" + configuration);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./NAudio.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./NAudio.sln", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild - unlikely to work, not tested
      XBuild("./NAudio.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    // https://cakebuild.net/api/Cake.Common.Tools.NUnit/NUnit3Settings/
    NUnit3("./**/bin/" + configuration + "/*Tests.dll", new NUnit3Settings {
        Where = "cat != IntegrationTest",
        NoResults = true
        });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);