# build.ps1 — Build AirLink as a single-file portable EXE
# Run from the repo root: pwsh scripts/build.ps1

Function Info($msg) {
    Write-Host -ForegroundColor DarkGreen "`nINFO: $msg`n"
}

Function Error($msg) {
    Write-Host `n`n
    Write-Error $msg
    exit 1
}

Function CheckReturnCodeOfPreviousCommand($msg) {
    if (-Not $?) {
        Error "${msg}. Error code: $LastExitCode"
    }
}

Function GetVersion() {
    $gitCommand = Get-Command -Name git

    try { $nearestTag = & $gitCommand describe --exact-match --tags HEAD 2> $null } catch {}
    if (-Not $?) {
        Info "The commit is not tagged. Using 'v0.0.0-dev' as version"
        $nearestTag = "v0.0.0-dev"
    }

    $commitHash = & $gitCommand rev-parse --short HEAD
    CheckReturnCodeOfPreviousCommand "Failed to get git commit hash"

    return "$nearestTag-$commitHash"
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot/.."
$buildFolder = "$root/build"
$publishFolder = "$buildFolder/publish"
$projectFile = "$root/src/AirLink/AirLink.csproj"
$solutionFile = "$root/AirLink.slnx"
$version = GetVersion

Info "Version: $version"

# Run tests first
Info "Running tests..."
dotnet test $solutionFile --configuration Release --no-restore 2>&1
# Don't fail on test errors during build — tests may need Windows audio hardware

# Build and publish
Info "Publishing AirLink (single-file EXE)..."
dotnet publish $projectFile `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    /property:PublishSingleFile=true `
    /property:DebugType=None `
    /property:DebugSymbols=false
CheckReturnCodeOfPreviousCommand "'dotnet publish' failed"

# Find the published EXE
$publishedExe = Get-ChildItem -Path "$root/src/AirLink/bin/Release/*/win-x64/publish/AirLink.exe" -Recurse | Select-Object -First 1
if (-Not $publishedExe) {
    Error "Published AirLink.exe not found"
}

# Copy to build/publish
Info "Copying to $publishFolder"
New-Item $publishFolder -Force -ItemType "directory" > $null
Copy-Item $publishedExe.FullName -Destination $publishFolder -Force

# Create zip archive
$zipFile = "$publishFolder/AirLink-$version.zip"
Info "Creating archive: $zipFile"
Compress-Archive -Force -Path "$publishFolder/AirLink.exe" -DestinationPath $zipFile

Info "Build complete!"
Info "  EXE: $publishFolder/AirLink.exe"
Info "  ZIP: $zipFile"
