function Get-ProgramFiles {
    #TODO: Someone please come up with a better way of detecting this - Tried http://msmvps.com/blogs/richardsiddaway/archive/2010/02/26/powershell-pack-mount-specialfolder.aspx and some enums missing
    #      This is needed because of this http://www.mattwrock.com/post/2012/02/29/What-you-should-know-about-running-ILMerge-on-Net-45-Beta-assemblies-targeting-Net-40.aspx (for machines that dont have .net 4.5 and only have 4.0)
    if (Test-Path "C:\Program Files (x86)") {
        return "C:\Program Files (x86)"
    }
    return "C:\Program Files"
}

function Get-AzureSdkVisualStudioVersion {
    if (Test-Path (((Get-ProgramFiles) + "\MSBuild\Microsoft\VisualStudio\v11.0\Windows Azure Tools"))) {
        return '11.0'
    }
    
    if (Test-Path (((Get-ProgramFiles) + "\MSBuild\Microsoft\VisualStudio\v12.0\Windows Azure Tools"))) {
        return '12.0'
    }

    throw 'No known Azure SDK installed'
}

function Get-Version {
    $thisVersion = $versionNumber;
    if (!$thisVersion) {
        $thisVersion = "0.0.1.0"
    }

    return $thisVersion
}

properties {
    $base_dir = resolve-path .
    $build_dir = "$base_dir\build"
    $source_dir = "$base_dir\"
    $package_dir = "$base_dir\packages"
    $framework_dir =  (Get-ProgramFiles) + "\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0"
    $config = "release"
    $versionNumber = $Env:TargetAssemblyVersion
    $visualStudioVersion = Get-AzureSdkVisualStudioVersion
}

framework('4.0')

task default -depends nupackage,package

task generate-build-files {
    $now = Get-Date
    $thisVersion = Get-Version

    "Creating version files"
    "  We are version ""$thisVersion"""

    $assemblyInfo = "// File generate during build (" + $now + ")

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion(""$thisVersion"")]
[assembly: AssemblyFileVersion(""$thisVersion"")]
"

    Write-Output $assemblyInfo > BuildGenerated\AssemblyInfo.cs
}

task compile-release -depends generate-build-files {
    "Compiling for release"
    "   Regard.Query.sln"
    
    exec { msbuild $base_dir\Regard.Query.sln /p:Configuration=release /verbosity:minimal /tv:4.0 /p:VisualStudioVersion=$visualStudioVersion }
}

task compile-debug -depends generate-build-files {
    "Compiling for debug"
    "   Regard.Query.sln"
    
    exec { msbuild $base_dir\Regard.Query.sln /p:Configuration=debug /verbosity:minimal /tv:4.0 /p:VisualStudioVersion=$visualStudioVersion }
}

task compile -depends compile-debug,compile-release

task nupackage -depends compile,test-release {
    "Packaging"
    "  Regard.Query.csproj"

    $version = Get-Version

    Set-Location Regard.Query
    Remove-Item .\bin\*.nupkg
    exec { ..\.nuget\NuGet.exe pack -OutputDirectory bin -Prop Configuration=Release -Version $version }
    Set-Location ..

    "  Regard.Query.WebAPI.csproj"

    Set-Location Regard.Query.WebAPI
    Remove-Item .\bin\*.nupkg
    exec { ..\.nuget\NuGet.exe pack -OutputDirectory bin -Prop Configuration=Release -Version $version }
    Set-Location ..
}

task test-release -depends compile {
    "Testing"
    
    exec { & $base_dir\packages\NUnit.Runners.2.6.3\tools\nunit-console.exe /labels $base_dir\Regard.Query.Tests\bin\release\Regard.Query.Tests.dll }
}

task package -depends compile,test-release {
    "Packaging"
    "   Regard.Query.Internal.Service.ccproj"

    exec { msbuild $base_dir\Regard.Query.Internal.Service\Regard.Query.Internal.Service.ccproj /t:Publish /p:Configuration=$config /verbosity:minimal /tv:4.0 /p:VisualStudioVersion=$visualStudioVersion }
}
