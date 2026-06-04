# POS Hardware Test App

Companion Windows client for testing the DeviceMocker POS emulator host.

## Purpose

This app is included for development and integration testing. It gives you a simple way to send printer and drawer-related ESC/POS traffic into DeviceMocker without needing a real POS application during early testing.

## What It Can Send

- ESC/POS receipt text
- drawer-kick commands
- cut-paper commands
- raw hex payloads
- test-only drawer reset command

## Recommended DeviceMocker Host Settings

In `DeviceMocker -> Emulators`, use:

- `Device Type = ReceiptPrinter`
- `Emulation Protocol = EscPos`
- `Drawer Kick Link Mode = PrinterDriven`
- `Transport Binding = Tcp`
- `TCP Binding IP = 127.0.0.1`
- `TCP Listening Port = 9100`

Then click `Start Host`.

## Run From Source

```powershell
dotnet run --project Samples\PosHardwareTestApp\PosHardwareTestApp.csproj
```

## Quick Test

1. Start the host in DeviceMocker.
2. Open `POS Hardware Test App`.
3. Leave transport on `TCP`.
4. Click `Print + Kick Drawer`.
5. Confirm DeviceMocker renders the receipt preview.
6. Confirm the drawer state changes to `Open`.
7. Click `Reset Drawer State` to return the emulator drawer to `Closed`.

## Notes

- `Reset Drawer State` is a DeviceMocker development command for emulator testing.
- It is not intended to represent a real hardware close-drawer signal.
