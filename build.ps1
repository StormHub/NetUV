$Configuration="Release";
$DotNetVersion = "2.1.302";
$DotNetInstallerUri = "https://raw.githubusercontent.com/dotnet/cli/rel/1.1.0/scripts/obtain/dotnet-install.ps1";

# Make sure tools folder exists
$PSScriptRoot = $pwd

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

Function Remove-PathVariable([string]$VariableToRemove)
{
    $path = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($path -ne $null)
    {
        $newItems = $path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "User")
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
    if ($path -ne $null)
    {
        $newItems = $path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "Process")
    }
}

# Get .NET Core CLI path if installed.
$FoundDotNetCliVersion = $null;
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $FoundDotNetCliVersion = dotnet --version;
}

if($FoundDotNetCliVersion -ne $DotNetVersion) {
    $InstallPath = Join-Path $PSScriptRoot ".dotnet"
    if (!(Test-Path $InstallPath)) {
        mkdir -Force $InstallPath | Out-Null;
    }
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallerUri, "$InstallPath\dotnet-install.ps1");
    & $InstallPath\dotnet-install.ps1 -Channel preview -Version $DotNetVersion -InstallDir $InstallPath;

    Remove-PathVariable "$InstallPath"
    $env:PATH = "$InstallPath;$env:PATH"
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    $env:DOTNET_CLI_TELEMETRY_OPTOUT=1

    & dotnet --info
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1

Write-Host "Restoring all packages"
Invoke-Expression "& dotnet restore"
if ($lastexitcode -ne 0) {
    Write-Error "Failed to restore packages."
    exit -1
}

$errorsEncountered = 0
$projectsFailed = New-Object System.Collections.Generic.List[String]

foreach ($file in [System.IO.Directory]::EnumerateFiles("$PSScriptRoot\src", "*.csproj", "AllDirectories")) {
    Write-Host "Building $file..."
    Invoke-Expression "& dotnet build $file -c $Configuration"

    if ($lastexitcode -ne 0) {
        Write-Error "Failed to build project $file"
        $projectsFailed.Add($file)
        $errorsEncountered++
    }
}

$file="./test/NetUV.Core.Tests/NetUV.Core.Tests.csproj"
Write-Host "Building and running tests for project $file..."
Invoke-Expression "& dotnet test $file -c $Configuration"

if ($lastexitcode -ne 0) {
    Write-Error "Some tests failed in project $file"
    $projectsFailed.Add($file)
    $errorsEncountered++
}

$file="./test/NetUV.Core.Tests.Performance/NetUV.Core.Tests.Performance.csproj"
Write-Host "Building and running performance tests for project $file..."
Invoke-Expression "& dotnet run -c $Configuration -f netcoreapp2.1 -p $file"

if ($lastexitcode -ne 0) {
    Write-Error "Performance tests failed in project $file"
    $projectsFailed.Add($file)
    $errorsEncountered++
}

if ($errorsEncountered -eq 0) {
    Write-Host "** Build succeeded. **" -foreground "green"
}
else {
    Write-Host "** Build failed. $errorsEncountered projects failed to build or test. **" -foreground "red"
    foreach ($file in $projectsFailed) {
        Write-Host "    $file" -foreground "red"
    }
}

exit $errorsEncountered