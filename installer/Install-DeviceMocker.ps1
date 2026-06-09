param(
    [switch]$Silent,
    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName PresentationFramework

$appName = "DeviceMocker"
$publisher = "x1n-Q"
$version = "1.1.0"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\$appName"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\$appName"
$payloadZip = Join-Path $PSScriptRoot "DeviceMocker-Payload.zip"
$uninstallScriptSource = Join-Path $PSScriptRoot "Uninstall-DeviceMocker.ps1"

function Show-Message {
    param(
        [string]$Message,
        [string]$Title,
        [System.Windows.MessageBoxButton]$Buttons = [System.Windows.MessageBoxButton]::OK,
        [System.Windows.MessageBoxImage]$Icon = [System.Windows.MessageBoxImage]::Information
    )

    return [System.Windows.MessageBox]::Show($Message, $Title, $Buttons, $Icon)
}

function Show-ErrorAndExit {
    param([string]$Message)

    if ($Silent) {
        Write-Error $Message
    }
    else {
        Show-Message -Message $Message -Title "Setup Failed" -Icon Error | Out-Null
    }

    exit 1
}

function New-Shortcut {
    param(
        [string]$ShortcutPath,
        [string]$TargetPath,
        [string]$WorkingDirectory,
        [string]$IconLocation
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.IconLocation = $IconLocation
    $shortcut.Save()
}

try {
    if (-not (Test-Path $payloadZip)) {
        throw "Installer payload was not found."
    }

    if (-not (Test-Path $uninstallScriptSource)) {
        throw "Uninstaller script was not found."
    }

    $runningApps = Get-Process DeviceMocker, PosHardwareTestApp -ErrorAction SilentlyContinue
    if ($runningApps) {
        Show-ErrorAndExit -Message "Please close DeviceMocker and POS Hardware Test App, then run setup again."
    }

    if (Test-Path $installDir) {
        Remove-Item $installDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    Expand-Archive -Path $payloadZip -DestinationPath $installDir -Force

    $uninstallScriptTarget = Join-Path $installDir "Uninstall-DeviceMocker.ps1"
    $uninstallCmdTarget = Join-Path $installDir "Uninstall-DeviceMocker.cmd"
    Copy-Item $uninstallScriptSource $uninstallScriptTarget -Force

    $uninstallCmdContent = @'
@echo off
setlocal
set "TEMP_SCRIPT=%TEMP%\DeviceMocker-Uninstall-%RANDOM%%RANDOM%.ps1"
copy /Y "%~dp0Uninstall-DeviceMocker.ps1" "%TEMP_SCRIPT%" >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%TEMP_SCRIPT%" -InstallDir "%~dp0"
del "%TEMP_SCRIPT%" >nul 2>&1
exit /b %errorlevel%
'@
    Set-Content -Path $uninstallCmdTarget -Value $uninstallCmdContent -Encoding ASCII

    if (Test-Path $startMenuDir) {
        Remove-Item $startMenuDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null

    $mainExePath = Join-Path $installDir "DeviceMocker.exe"
    $mainIconPath = $mainExePath
    $startShortcutPath = Join-Path $startMenuDir "DeviceMocker.lnk"
    $uninstallShortcutPath = Join-Path $startMenuDir "Uninstall DeviceMocker.lnk"

    New-Shortcut -ShortcutPath $startShortcutPath -TargetPath $mainExePath -WorkingDirectory $installDir -IconLocation $mainIconPath
    New-Shortcut -ShortcutPath $uninstallShortcutPath -TargetPath $uninstallCmdTarget -WorkingDirectory $installDir -IconLocation $mainIconPath

    $uninstallKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$appName"
    if (Test-Path $uninstallKeyPath) {
        Remove-Item $uninstallKeyPath -Recurse -Force
    }

    $estimatedSizeKb = [int]((Get-ChildItem $installDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1KB)
    New-Item -Path $uninstallKeyPath -Force | Out-Null
    Set-ItemProperty -Path $uninstallKeyPath -Name "DisplayName" -Value $appName
    Set-ItemProperty -Path $uninstallKeyPath -Name "DisplayVersion" -Value $version
    Set-ItemProperty -Path $uninstallKeyPath -Name "Publisher" -Value $publisher
    Set-ItemProperty -Path $uninstallKeyPath -Name "InstallLocation" -Value $installDir
    Set-ItemProperty -Path $uninstallKeyPath -Name "DisplayIcon" -Value $mainExePath
    Set-ItemProperty -Path $uninstallKeyPath -Name "UninstallString" -Value $uninstallCmdTarget
    Set-ItemProperty -Path $uninstallKeyPath -Name "QuietUninstallString" -Value $uninstallCmdTarget
    Set-ItemProperty -Path $uninstallKeyPath -Name "URLInfoAbout" -Value "https://github.com/x1n-Q/DeviceMocker"
    Set-ItemProperty -Path $uninstallKeyPath -Name "NoModify" -Value 1 -Type DWord
    Set-ItemProperty -Path $uninstallKeyPath -Name "NoRepair" -Value 1 -Type DWord
    Set-ItemProperty -Path $uninstallKeyPath -Name "EstimatedSize" -Value $estimatedSizeKb -Type DWord

    if ($LaunchAfterInstall) {
        Start-Process -FilePath $mainExePath -WorkingDirectory $installDir
    }
    elseif (-not $Silent) {
        $launchNow = Show-Message -Message "DeviceMocker was installed successfully.`n`nOpen DeviceMocker now?" -Title "Setup Complete" -Buttons YesNo -Icon Information
        if ($launchNow -eq [System.Windows.MessageBoxResult]::Yes) {
            Start-Process -FilePath $mainExePath -WorkingDirectory $installDir
        }
    }

    exit 0
}
catch {
    if ($Silent) {
        Write-Error $_.Exception.Message
    }
    else {
        Show-Message -Message ("Installation failed.`n`n" + $_.Exception.Message) -Title "Setup Failed" -Icon Error | Out-Null
    }

    exit 1
}
