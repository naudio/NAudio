#r "packages/FAKE/tools/FakeLib.dll"
open Fake 
open System.IO

let buildDir = "" // using the defaults build output
let appReferences = !! "./NAudio.sln" // still building from the solution

let deployDir = "./BuildArtefacts/"
let testDir = "./NAudioTests/bin/Debug/"
let testDlls = !! (testDir + "*Tests.dll")

let buildLogo = """  _   _    _             _ _       
 | \ | |  / \  _   _  __| (_) ___  
 |  \| | / _ \| | | |/ _` | |/ _ \ 
 | |\  |/ ___ \ |_| | (_| | | (_) |
 |_| \_/_/   \_\__,_|\__,_|_|\___/ 
"""                                   

traceHeader buildLogo

Target "DebugBuild" (fun _ ->
    MSBuildDebug buildDir "Build" appReferences
        |> Log "Build output: "
)

Target "ReleaseBuild" (fun _ ->
    MSBuildRelease buildDir "Build" appReferences
        |> Log "Build output: "
)

Target "Test" (fun _ ->
    trace "Running unit tests"
    testDlls
    |> NUnit (fun p -> 
        {p with
            ExcludeCategory = "IntegrationTest";
            DisableShadowCopy = true; 
            OutputFile = testDir + "TestResults.xml"})
)

Target "Clean" (fun _ ->
    trace "Cleaning up"
    MSBuildDebug buildDir "Clean" appReferences
        |> Log "Debug clean: "
    MSBuildRelease buildDir "Clean" appReferences
        |> Log "Release clean: "
    CleanDirs [deployDir]
)

Target "NuGet" (fun _ ->
    (*NuGetDefaults() 
        |> sprintf "%A"
        |> trace*)
    NuGet (fun p -> 
        {p with
            (*Authors = authors
            Project = projectName
            Description = projectDescription                               
            Summary = projectSummary
            WorkingDir = packagingDir
            AccessKey = myAccesskey*)
            Version = "1.8.0-beta" // todo get the version number from elsewhere
            WorkingDir = "."
            OutputPath = deployDir
            
            Publish = false }) 
            "NAudio.nuspec"

    NuGet (fun p -> 
        {p with
            Version = "1.0.1"
            WorkingDir = "."
            OutputPath = deployDir
            
            Publish = false }) 
            "NAudio.Wma.nuspec"

)

Target "Release" DoNothing

Target "ZipAll" DoNothing

// a bit hacky, but persuading CreateZipOfIncludes to create the directory structure we want
let demoIncludes = 
    !! "**"
    -- "**/*.pdb"
    -- "*.vshost.*"
                    
let audioFileInspectorBaseDirectory = Path.GetFullPath "./AudioFileInspector/bin/Debug"
let audioFileInspectorIncludes = { demoIncludes with BaseDirectory = audioFileInspectorBaseDirectory }

let naudioDemoBaseDirectory = Path.GetFullPath "./NAudioDemo/bin/Debug"
let naudioDemoIncludes = { demoIncludes with BaseDirectory = naudioDemoBaseDirectory }

let wpfDemoBaseDirectory = Path.GetFullPath "./NAudioWpfDemo/bin/Debug"
let wpfDemoIncludes = { demoIncludes with BaseDirectory = wpfDemoBaseDirectory }


let demoFiles = [ "AudioFileInspector", audioFileInspectorIncludes;
        "NAudioDemo", naudioDemoIncludes;
        "NAudioWpfDemo", wpfDemoIncludes
]

Target "ZipDemo" (fun _ ->
    trace audioFileInspectorBaseDirectory
    CreateZipOfIncludes (deployDir + "NAudio-Demos.zip") "" DefaultZipLevel demoFiles        
)

Target "ZipSource" (fun _ ->
    let errorCode = Shell.Exec( "git","archive --format zip --output " + deployDir + "NAudio-Source.zip master", ".")
    ()
)

// Create a zip release library
Target "ZipLib" (fun _ ->
    let zipFiles = [@".\NAudio\bin\Release\NAudio.dll";
        @".\NAudio\bin\Release\NAudio.xml";
        @".\NAudio.WindowsMediaFormat\bin\Release\NAudio.WindowsMediaFormat.dll";
        "license.txt";
        "readme.txt"
        ]
    let flatten = true
    let comment = ""
    let workingDir = "."
    CreateZip workingDir (deployDir + "NAudio-Release.zip") comment DefaultZipLevel flatten zipFiles
)

"Clean" 
    ==> "DebugBuild"
    ==> "Test"
    ?=> "ReleaseBuild"
    ==> "Release"



"ZipDemo" ==> "ZipAll"
"ZipLib" ==> "ZipAll"
"ZipSource" ==> "ZipAll"

"ReleaseBuild" ==> "ZipLib" 

RunTargetOrDefault "Test"