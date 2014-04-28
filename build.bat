@echo off

rem Set NuGetSources to something else if you want to use dependencies from a different server
if "%NuGetSources%"=="" set NuGetSources=https://www.nuget.org/api/v2

.nuget\NuGet.exe install .nuget\packages.config -OutputDirectory packages -source "%NuGetSources%" 

.nuget\NuGet.exe restore Regard.Query.sln -source "%NuGetSources%" 
powershell.exe -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module '.\packages\psake.*\tools\psake.psm1'; invoke-psake .\build.ps1 %*; if ($LastExitCode -ne 0) {write-host "ERROR: $LastExitCode" -fore RED; exit $lastexitcode} }"
