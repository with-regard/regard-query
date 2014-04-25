function Get-ProgramFiles
{
    #TODO: Someone please come up with a better way of detecting this - Tried http://msmvps.com/blogs/richardsiddaway/archive/2010/02/26/powershell-pack-mount-specialfolder.aspx and some enums missing
    #      This is needed because of this http://www.mattwrock.com/post/2012/02/29/What-you-should-know-about-running-ILMerge-on-Net-45-Beta-assemblies-targeting-Net-40.aspx (for machines that dont have .net 4.5 and only have 4.0)
    if (Test-Path "C:\Program Files (x86)") {
        return "C:\Program Files (x86)"
    }
    return "C:\Program Files"
}

function Get-AzureSdkVisualStudioVersion
{
    if (Test-Path (((Get-ProgramFiles) + "\MSBuild\Microsoft\VisualStudio\v11.0\Windows Azure Tools"))) {
        return '11.0'
    }
    
    if (Test-Path (((Get-ProgramFiles) + "\MSBuild\Microsoft\VisualStudio\v12.0\Windows Azure Tools"))) {
        return '12.0'
    }

    throw 'No known Azure SDK installed'
}

properties {
    $base_dir = resolve-path .
    $build_dir = "$base_dir\build"
    $source_dir = "$base_dir\"
    $package_dir = "$base_dir\packages"
    $framework_dir =  (Get-ProgramFiles) + "\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0"
    $config = "release"
    $visualStudioVersion = Get-AzureSdkVisualStudioVersion
}

framework('4.0')

task default -depends nupackage

task compile-release {
    "Compiling for release"
    "   Regard.Query.sln"
    
    exec { msbuild $base_dir\Regard.Query.sln /p:Configuration=release /verbosity:minimal /tv:4.0 /p:VisualStudioVersion=$visualStudioVersion }
}

task compile-debug {
    "Compiling for debug"
    "   Regard.Query.sln"
    
    exec { msbuild $base_dir\Regard.Query.sln /p:Configuration=debug /verbosity:minimal /tv:4.0 /p:VisualStudioVersion=$visualStudioVersion }
}

task compile -depends compile-debug,compile-release

task nupackage -depends compile {
    "Packaging"
    "  Regard.Query.csproj"

    cd Regard.Query
    exec { ..\.nuget\NuGet.exe pack -OutputDirectory bin }
}

task package -depends compile {
    "Packaging"
    "   Regard.Query.sln"
}
