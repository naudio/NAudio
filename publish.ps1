# publishes to NuGet
# $apiKey needs to be already set up with NuGet publishing key
$packages = "NAudio.Core", "NAudio.Asio", "NAudio.WinForms", "NAudio.Midi", 
            "NAudio.WinMM", "NAudio.Wasapi", "NAudio.Uap",
            "NAudio", "NAudio.Extras"

foreach ($package in $packages)
{
    # publish the most recently created .nupkg file
    $folder = "$package\bin\Release"
    $recent = gci "$folder\*.nupkg" | sort LastWriteTime | select -last 1
    $pkg = $recent.Name
    # note that this will fail with 409 error if you try to push package that already exists
    dotnet nuget push "$folder\$pkg" --api-key $apiKey --source https://api.nuget.org/v3/index.json
}