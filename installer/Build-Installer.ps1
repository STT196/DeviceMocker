param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "DeviceMocker\DeviceMocker.csproj"
[xml]$projectXml = Get-Content $projectPath
$version = [string]($projectXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Unable to read the application version from DeviceMocker.csproj."
}

$artifactsRoot = Join-Path $repoRoot "artifacts\installer"
$publishDir = Join-Path $artifactsRoot "publish"
$stagingDir = Join-Path $artifactsRoot "staging"
$outputDir = Join-Path $repoRoot "releases"
$payloadZip = Join-Path $stagingDir "DeviceMocker-Payload.zip"
$sedPath = Join-Path $stagingDir "DeviceMocker-Setup.sed"
$setupExe = Join-Path $outputDir ("DeviceMocker-Setup-v{0}-{1}.exe" -f $version, $RuntimeIdentifier)

if (Test-Path $artifactsRoot) {
    Remove-Item $artifactsRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host "Publishing DeviceMocker..." -ForegroundColor Cyan
dotnet publish $projectPath `
    -c $Configuration `
    -r $RuntimeIdentifier `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if (-not (Test-Path (Join-Path $publishDir "DeviceMocker.exe"))) {
    throw "Publish output is missing DeviceMocker.exe."
}

if (-not (Test-Path (Join-Path $publishDir "PosHardwareTestApp\PosHardwareTestApp.exe"))) {
    throw "Publish output is missing the bundled POS Hardware Test App."
}

Write-Host "Preparing installer payload..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $payloadZip -CompressionLevel Optimal

Copy-Item (Join-Path $PSScriptRoot "Install-DeviceMocker.ps1") $stagingDir -Force
Copy-Item (Join-Path $PSScriptRoot "Install-DeviceMocker.cmd") $stagingDir -Force
Copy-Item (Join-Path $PSScriptRoot "Uninstall-DeviceMocker.ps1") $stagingDir -Force

$stagingDirWindows = $stagingDir
$setupExeWindows = $setupExe

$sedContent = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$setupExeWindows
FriendlyName=DeviceMocker Setup v$version
AppLaunched=Install-DeviceMocker.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[SourceFiles]
SourceFiles0=$stagingDirWindows\
[SourceFiles0]
%FILE0%=DeviceMocker-Payload.zip
%FILE1%=Install-DeviceMocker.ps1
%FILE2%=Install-DeviceMocker.cmd
%FILE3%=Uninstall-DeviceMocker.ps1
[Strings]
FILE0=DeviceMocker-Payload.zip
FILE1=Install-DeviceMocker.ps1
FILE2=Install-DeviceMocker.cmd
FILE3=Uninstall-DeviceMocker.ps1
"@
Set-Content -Path $sedPath -Value $sedContent -Encoding ASCII

Write-Host "Building installer..." -ForegroundColor Cyan
$iexpress = Start-Process -FilePath "C:\Windows\System32\iexpress.exe" -ArgumentList "/N", $sedPath -PassThru

$deadline = (Get-Date).AddMinutes(10)
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $setupExe) -and ((Get-Item $setupExe).Length -gt 0)) {
        break
    }

    Start-Sleep -Seconds 2
}

if (-not (Test-Path $setupExe)) {
    throw "IExpress did not produce the setup executable."
}

if (-not $iexpress.HasExited) {
    Stop-Process -Id $iexpress.Id -Force
}

Write-Host ("Installer ready: {0}" -f $setupExe) -ForegroundColor Green
