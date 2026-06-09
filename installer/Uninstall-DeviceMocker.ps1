param(
    [string]$InstallDir = "",
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName PresentationFramework

$appName = "DeviceMocker"

function Show-Message {
    param(
        [string]$Message,
        [string]$Title,
        [System.Windows.MessageBoxButton]$Buttons = [System.Windows.MessageBoxButton]::OK,
        [System.Windows.MessageBoxImage]$Icon = [System.Windows.MessageBoxImage]::Information
    )

    return [System.Windows.MessageBox]::Show($Message, $Title, $Buttons, $Icon)
}

try {
    if ([string]::IsNullOrWhiteSpace($InstallDir)) {
        $InstallDir = Join-Path $env:LOCALAPPDATA "Programs\$appName"
    }

    $InstallDir = $InstallDir.TrimEnd('\')
    $startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\$appName"
    $uninstallKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$appName"

    $runningApps = Get-Process DeviceMocker, PosHardwareTestApp -ErrorAction SilentlyContinue
    if ($runningApps) {
        if (-not $Silent) {
            $closeApps = Show-Message -Message "DeviceMocker is still running.`n`nClose the app automatically and continue uninstall?" -Title "Close Running Apps" -Buttons YesNo -Icon Question
            if ($closeApps -ne [System.Windows.MessageBoxResult]::Yes) {
                exit 1
            }
        }

        $runningApps | Stop-Process -Force
        Start-Sleep -Seconds 1
    }

    if (Test-Path $startMenuDir) {
        Remove-Item $startMenuDir -Recurse -Force
    }

    if (Test-Path $uninstallKeyPath) {
        Remove-Item $uninstallKeyPath -Recurse -Force
    }

    $cleanupScriptPath = Join-Path $env:TEMP ("DeviceMocker-Cleanup-" + [Guid]::NewGuid().ToString("N") + ".cmd")
    $cleanupScript = @"
@echo off
ping 127.0.0.1 -n 3 > nul
if exist "$InstallDir" rmdir /s /q "$InstallDir"
del "%~f0" >nul 2>&1
"@
    Set-Content -Path $cleanupScriptPath -Value $cleanupScript -Encoding ASCII
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$cleanupScriptPath`"" -WindowStyle Hidden

    if (-not $Silent) {
        Show-Message -Message "DeviceMocker was removed from this PC." -Title "Uninstall Complete" -Icon Information | Out-Null
    }

    exit 0
}
catch {
    if ($Silent) {
        Write-Error $_.Exception.Message
    }
    else {
        Show-Message -Message ("Uninstall failed.`n`n" + $_.Exception.Message) -Title "Uninstall Failed" -Icon Error | Out-Null
    }

    exit 1
}
