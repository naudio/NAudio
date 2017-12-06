REM can call with target eg
REM build zipall
REM build nuget
@echo off
cls
if not exist "packages\FAKE" "Tools\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"
"packages\FAKE\tools\Fake.exe" build.fsx %*
pause
