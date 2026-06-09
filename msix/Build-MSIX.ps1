param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("x64")]
    [string]$Platform = "x64",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Publisher = "CN=Daniel Depaor",
    [string]$Version = "1.1.0.0",
    [switch]$SignPackage
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$packageProject = Join-Path $repoRoot "DeviceMocker.Package\DeviceMocker.Package.wapproj"
$manifestTemplate = Join-Path $repoRoot "DeviceMocker.Package\Package.appxmanifest"
$publishRoot = Join-Path $repoRoot "artifacts\publish\DeviceMocker-$RuntimeIdentifier"
$msixRoot = Join-Path $repoRoot "artifacts\msix"
$layoutRoot = Join-Path $msixRoot "layout\$RuntimeIdentifier"
$imagesRoot = Join-Path $repoRoot "DeviceMocker.Package\Images"
$packageName = "DeviceMocker_$Version`_$Platform.msix"
$packagePath = Join-Path $msixRoot $packageName
$certificateBaseName = "DeviceMocker-TestCert"
$pfxPath = Join-Path $msixRoot "$certificateBaseName.pfx"
$cerPath = Join-Path $msixRoot "$certificateBaseName.cer"
$certificatePassword = "DeviceMocker123!"

function Find-FirstFile {
    param(
        [string[]]$Roots,
        [string]$Filter
    )

    foreach ($root in $Roots) {
        if (-not (Test-Path $root)) {
            continue
        }

        $match = Get-ChildItem $root -Recurse -Filter $Filter -ErrorAction SilentlyContinue |
            Select-Object -First 1

        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

function Find-MsBuild {
    return Find-FirstFile -Roots @(
        "C:\Program Files\Microsoft Visual Studio",
        "C:\Program Files (x86)\Microsoft Visual Studio"
    ) -Filter "MSBuild.exe"
}

function Find-DesktopBridgeProps {
    return Find-FirstFile -Roots @(
        "C:\Program Files\Microsoft Visual Studio",
        "C:\Program Files (x86)\Microsoft Visual Studio"
    ) -Filter "Microsoft.DesktopBridge.props"
}

function Find-SdkTool {
    param([string]$FileName)

    $candidateRoots = @(
        "C:\Program Files (x86)\Windows Kits\10\bin",
        "C:\Program Files\Windows Kits\10\bin"
    )

    foreach ($root in $candidateRoots) {
        if (-not (Test-Path $root)) {
            continue
        }

        $preferred = Get-ChildItem $root -Recurse -Filter $FileName -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*\\x64\\$FileName" } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($preferred) {
            return $preferred.FullName
        }

        $fallback = Get-ChildItem $root -Recurse -Filter $FileName -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($fallback) {
            return $fallback.FullName
        }
    }

    return $null
}

function New-PackageManifest {
    param(
        [string]$TemplatePath,
        [string]$OutputPath,
        [string]$ManifestPublisher,
        [string]$ManifestVersion
    )

    $manifest = [IO.File]::ReadAllText($TemplatePath)
    $manifest = $manifest.Replace("CN=REPLACE_WITH_PARTNER_CENTER_PUBLISHER", $ManifestPublisher)
    $manifest = $manifest.Replace('Version="1.1.0.0"', "Version=`"$ManifestVersion`"")
    $manifest = $manifest.Replace('$targetnametoken$.exe', 'DeviceMocker.exe')
    $manifest = $manifest.Replace('$targetentrypoint$', 'windows.fullTrustApplication')

    [IO.File]::WriteAllText($OutputPath, $manifest, [Text.UTF8Encoding]::new($false))
}

function New-TestCertificate {
    param(
        [string]$Subject,
        [string]$PfxOutputPath,
        [string]$CerOutputPath,
        [string]$Password
    )

    $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
    $certificate = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $Subject } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $certificate) {
        $certificate = New-SelfSignedCertificate `
            -Type CodeSigningCert `
            -Subject $Subject `
            -FriendlyName "DeviceMocker Local Test" `
            -CertStoreLocation "Cert:\CurrentUser\My"
    }

    Export-PfxCertificate -Cert $certificate -FilePath $PfxOutputPath -Password $securePassword | Out-Null
    Export-Certificate -Cert $certificate -FilePath $CerOutputPath | Out-Null
}

function Build-WithWapProject {
    param(
        [string]$MsBuildPath,
        [string]$ProjectPath,
        [string]$BuildConfiguration,
        [string]$BuildPlatform
    )

    Write-Host "Using Visual Studio packaging targets." -ForegroundColor Cyan
    Write-Host $MsBuildPath

    & $MsBuildPath $ProjectPath `
        /restore `
        /p:Configuration=$BuildConfiguration `
        /p:Platform=$BuildPlatform `
        /p:UapAppxPackageBuildMode=StoreUpload `
        /p:GenerateAppInstallerFile=false

    if ($LASTEXITCODE -ne 0) {
        throw "MSIX build failed via Windows Application Packaging Project."
    }
}

function Build-WithSdkTools {
    param(
        [string]$MakePriPath,
        [string]$MakeAppxPath
    )

    Write-Host "Visual Studio packaging targets were not found." -ForegroundColor Yellow
    Write-Host "Falling back to direct Windows SDK MSIX packaging." -ForegroundColor Yellow

    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $msixRoot -Force | Out-Null

    Write-Host "Publishing DeviceMocker..." -ForegroundColor Cyan
    & dotnet publish (Join-Path $repoRoot "DeviceMocker\DeviceMocker.csproj") `
        -c $Configuration `
        -r $RuntimeIdentifier `
        -p:SelfContained=true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $publishRoot

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    if (Test-Path $layoutRoot) {
        Remove-Item $layoutRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $layoutRoot | Out-Null
    Copy-Item (Join-Path $publishRoot "*") $layoutRoot -Recurse -Force
    New-Item -ItemType Directory -Path (Join-Path $layoutRoot "Images") | Out-Null
    Copy-Item (Join-Path $imagesRoot "*") (Join-Path $layoutRoot "Images") -Recurse -Force

    $layoutManifest = Join-Path $layoutRoot "AppxManifest.xml"
    New-PackageManifest `
        -TemplatePath $manifestTemplate `
        -OutputPath $layoutManifest `
        -ManifestPublisher $Publisher `
        -ManifestVersion $Version

    $priConfigPath = Join-Path $layoutRoot "priconfig.xml"
    & $MakePriPath createconfig /cf $priConfigPath /dq en-US /pv 10.0.0 /o
    if ($LASTEXITCODE -ne 0) {
        throw "makepri createconfig failed."
    }

    & $MakePriPath new `
        /pr $layoutRoot `
        /cf $priConfigPath `
        /mn $layoutManifest `
        /of (Join-Path $layoutRoot "resources.pri") `
        /o

    if ($LASTEXITCODE -ne 0) {
        throw "makepri new failed."
    }

    if (Test-Path $packagePath) {
        Remove-Item $packagePath -Force
    }

    & $MakeAppxPath pack /d $layoutRoot /p $packagePath /o
    if ($LASTEXITCODE -ne 0) {
        throw "makeappx pack failed."
    }
}

$msbuild = Find-MsBuild
$desktopBridgeProps = Find-DesktopBridgeProps
$makePri = Find-SdkTool -FileName "makepri.exe"
$makeAppx = Find-SdkTool -FileName "makeappx.exe"
$signTool = Find-SdkTool -FileName "signtool.exe"

if (-not $makePri -or -not $makeAppx) {
    Write-Host "Windows SDK packaging tools are missing." -ForegroundColor Red
    Write-Host "Install the Windows 10/11 SDK so MakePri.exe and MakeAppx.exe are available."
    exit 1
}

if ($msbuild -and $desktopBridgeProps) {
    Build-WithWapProject `
        -MsBuildPath $msbuild `
        -ProjectPath $packageProject `
        -BuildConfiguration $Configuration `
        -BuildPlatform $Platform
} else {
    Build-WithSdkTools `
        -MakePriPath $makePri `
        -MakeAppxPath $makeAppx
}

if ($SignPackage) {
    if (-not $signTool) {
        throw "signtool.exe was not found, so the MSIX package cannot be signed."
    }

    Write-Host "Signing local test package..." -ForegroundColor Cyan
    New-TestCertificate `
        -Subject $Publisher `
        -PfxOutputPath $pfxPath `
        -CerOutputPath $cerPath `
        -Password $certificatePassword

    & $signTool sign `
        /fd SHA256 `
        /f $pfxPath `
        /p $certificatePassword `
        $packagePath

    if ($LASTEXITCODE -ne 0) {
        throw "signtool sign failed."
    }

    Write-Host "Local signing certificate:" -ForegroundColor Cyan
    Write-Host $cerPath
}

Write-Host ""
Write-Host "MSIX package ready:" -ForegroundColor Green
Write-Host $packagePath
Write-Host ""
Write-Host "Publisher used for this build:" -ForegroundColor Cyan
Write-Host $Publisher
Write-Host ""
Write-Host "For Microsoft Store submission, rebuild with your exact Partner Center publisher string." -ForegroundColor Yellow
