Framework "4.0"
FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

Properties {
    $baseDir = resolve-path .
    $sourceDir = "$baseDir\src"
	$testDir = "$baseDir\tests"

    $packageDir = "$baseDir\package"
    
	$projectName = "NanoCluster"
    $solutionName = "Solution"
    $configurations = @("Debug","Release","DebugContracts")
	$projectConfig = $configurations[0]
    

    # if not provided, default to 1.0.0.0
    if(!$version)
    {
        $version = "0.1.0.0"
    }
    # tools
    # change testExecutable as needed, defaults to mstest
    $testExecutable = "$baseDir\packages\NUnit.Runners.2.6.4\tools\nunit-console-x86.exe"
    
    $unitTestProject = "NanoCluster.Tests"
    
    $nugetExecutable = "$baseDir\.nuget\nuget.exe"
	$nuspecFile = "$sourceDir\NanoCluster.nuspec"
	$nugetOutDir = "packaging\"
}

# default task
task default -Depends Compile

task Build -depends Compile {}

task Compile {
    Write-Host "Building main solution ($projectConfig)" -ForegroundColor Green
    exec { msbuild /nologo /m /nr:false /v:m /p:Configuration=$projectConfig $baseDir\$solutionName.sln }
}

task Test {
	Write-Host "Executing unit tests ($testExecutable)"

	$unitTestAssembly = "$testDir\$unitTestProject\bin\$projectConfig\$unitTestProject.dll"
	exec { & $testExecutable $unitTestAssembly /nologo /nodots /xml=$baseDir\tests_results.xml }
}

task BuildPackage -Depends Release{
	New-Item -Force -ItemType directory -Path $nugetOutDir
	exec { & "$nugetExecutable" pack $nuspecFile -Version $version -OutputDirectory $nugetOutDir }
}

task Release {
    Invoke-psake -nologo -properties @{"projectConfig"="Release"} Compile
}

task Clean {
    Write-Host "Cleaning main solution" -ForegroundColor Green
    foreach ($c in $configurations)
    {
        Write-Host "Cleaning ($c)"
        exec { msbuild /t:Clean /nologo /m /nr:false /v:m /p:Configuration=$c $baseDir\$solutionName.sln }
    }
	
	Write-Host "Removing nuget packages"
	Remove-Item $sourceDir\packages\* -exclude repositories.config -recurse
	Remove-Item $baseDir\packaging\*.nupkg
    
    Write-Host "Deleting the test directories"
	if (Test-Path $testDir)
    {
		Remove-Item $testDir -recurse -force
	}
}
