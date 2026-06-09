param(
    [string]$PackagePath = (Join-Path $PSScriptRoot "..\artifacts\msix\DeviceMocker_1.1.0.0_x64.msix"),
    [string]$CertificatePath = (Join-Path $PSScriptRoot "..\artifacts\msix\DeviceMocker-TestCert.cer")
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$resolvedPackage = (Resolve-Path $PackagePath).Path
$resolvedCertificate = (Resolve-Path $CertificatePath).Path

if (-not (Test-IsAdministrator)) {
    Write-Host "This install script should be run in an elevated PowerShell window." -ForegroundColor Yellow
    Write-Host "The MSIX package is signed, but Windows typically requires the signing certificate to be trusted at the machine level."
    Write-Host ""
    Write-Host "Package:" -ForegroundColor Cyan
    Write-Host $resolvedPackage
    Write-Host ""
    Write-Host "Certificate:" -ForegroundColor Cyan
    Write-Host $resolvedCertificate
    exit 1
}

Import-Certificate -FilePath $resolvedCertificate -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
Import-Certificate -FilePath $resolvedCertificate -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
Add-AppxPackage -Path $resolvedPackage -ForceApplicationShutdown

Write-Host "DeviceMocker MSIX installed successfully." -ForegroundColor Green
