@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-DeviceMocker.ps1"
exit /b %errorlevel%
