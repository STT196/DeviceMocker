# DeviceMocker

**Hardware Input Device Simulator for Developers**

A Windows desktop tool that simulates hardware input devices — barcode scanners, RFID readers, weighing scales, card readers, serial devices, and more — so developers can test their software without buying or connecting real hardware.

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6.svg)]()

---

## Why DeviceMocker?

If you're building POS software, inventory systems, access control, kiosk apps, or any application that depends on external hardware — you know the pain:

- **No scanner?** Can't test barcode input.
- **No scale?** Can't test weight readings.
- **No RFID reader?** Can't test card taps.
- **No serial device?** Can't test COM port communication.

**DeviceMocker solves this.** It simulates all these devices and sends data to your application exactly like real hardware would.

---

## Features

### 8 Device Simulators

| Device | What it does |
|--------|-------------|
| **Barcode / QR Scanner** | Sends barcodes via keyboard wedge. 20 sample barcodes, random generator, batch scan mode. |
| **Virtual Keyboard** | Sends individual keys, shortcuts (Ctrl+C, etc.), function keys F1-F12, arrow keys. |
| **Custom Button Panel** | POS-style configurable buttons. Cash, Card, Discount, Submit, etc. |
| **Serial Text Sender** | Sends text to COM ports. Built-in simulation mode with 5 virtual devices. |
| **Weighing Scale** | Sends weight values in standard scale formats (ST,GS protocol). kg/lb/g/oz. |
| **RFID / NFC Reader** | Sends card UIDs. 8 sample cards, hex/decimal formats, random UID generator. |
| **Magstripe Card Reader** | Sends track 1/2/3 data. Visa, MasterCard, Amex test cards included. |
| **Test Sequence Builder** | Build and replay multi-step action sequences. 3 presets included. |

### 5 Output Channels

| Channel | Description |
|---------|-------------|
| **Keyboard Wedge** | Types into the active window using Windows SendInput API |
| **Serial (COM Port)** | Sends data to a physical or virtual COM port |
| **TCP Client** | Sends data to a TCP server |
| **UDP** | Sends UDP datagrams |
| **HTTP Webhook** | POSTs JSON payload to any URL |

### Additional Features

- **Profile Management** — Save, load, import/export device profiles as JSON
- **Activity Logs** — Every action logged with timestamp, device, channel, payload, status
- **Log Export** — Export logs to CSV or JSON
- **Dark / Light Theme** — Toggle from Settings
- **Send After Countdown** — 3-second delay so you can switch to your target app
- **Batch Scan Mode** — Fire multiple barcodes automatically with configurable interval

---

## Quick Start

### Prerequisites

- **Windows 10/11**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (optional, for development)

### Build & Run

```bash
git clone https://github.com/x1n-Q/DeviceMocker.git
cd DeviceMocker/DeviceMocker
dotnet run
```

### Your First Test (Scanner → Notepad)

1. Open **Notepad**
2. Open **DeviceMocker** → **Devices** → **Barcode / QR Scanner**
3. Click **"Coca-Cola 330ml"** from the sample barcodes on the right
4. You get **3 seconds** — click inside Notepad
5. Notepad receives `5449000000996` + Enter — **exactly like a real scanner**

---

## How It Works

```
Device Module  →  Input Router  →  Output Channel  →  Target Application
```

**Example:**
```
Barcode Scanner Simulator
  → creates DeviceAction (payload: "4801234567890", suffix: "Enter")
  → InputRouter routes to KeyboardOutputService
  → SendInput API types the barcode into the active window
  → Your POS app receives it as if a real scanner was plugged in
```

The architecture separates devices from output channels, so the same barcode data can be sent via keyboard, serial, TCP, UDP, or HTTP.

---

## Device Guides

### Barcode / QR Scanner

Best for: POS systems, inventory apps, warehouse software.

1. Go to **Devices → Barcode / QR Scanner**
2. **Click a sample barcode** from the right panel, or type your own
3. Select suffix: **Enter** (most common for scanners), **Tab**, or **None**
4. Click **Send After 3s** → switch to your app
5. The barcode types into your app's focused input field

**Batch Mode:** Enable batch scan, set count and interval, click Start — fires multiple random barcodes automatically.

### Weighing Scale

Best for: Inventory software, shipping apps, food service systems.

1. Go to **Devices → Weighing Scale**
2. Set weight value, unit (kg/lb/g/oz), and format
3. **Standard format:** `ST,GS,+     5.00  kg` (matches AND, Ohaus, CAS scales)
4. **Raw Number:** `5.00` (for apps that parse just the number)
5. Click **Send After 3s** → switch to your app's weight field

### RFID / NFC Reader

Best for: Access control, attendance systems, membership apps.

1. Go to **Devices → RFID / NFC Reader**
2. Click a sample card or generate a random UID
3. Choose format: hex uppercase, lowercase, decimal, with colons, with spaces
4. Click **Tap After 3s** → switch to your app

### Serial Text Sender

Best for: Testing COM port communication without hardware.

**Simulation Mode (no hardware needed):**
1. Go to **Devices → Serial Text Sender** — Simulation Mode is ON by default
2. Select a virtual device (Echo, Weighing Scale, Barcode Scanner, Temperature Sensor, Access Control)
3. Click quick commands (READ, STATUS, WEIGHT) or type your own
4. Watch the terminal show TX → RX with hex bytes

**Hardware Mode:**
1. Uncheck Simulation Mode
2. Select COM port, baud rate, line ending
3. Click Connect → type payload → Send

### Test Sequence Builder

Best for: Automated testing, form filling, login sequences.

1. Go to **Devices → Test Sequence Builder**
2. Load a preset: **POS Login**, **Scan 3 Items**, or **Form Fill**
3. Or build your own: add steps with payload, type (Text/Key/Shortcut), suffix, and delay
4. Click **Run Sequence** → switch to your app
5. All steps execute in order with configured delays

---

## Project Structure

```
DeviceMocker/
├── Core/              — ServiceLocator, InputRouter, OutputChannelManager, DeviceManager
├── Models/            — DeviceAction, DeviceProfile, DeviceLog, OutputResult, enums
├── Interfaces/        — IDeviceModule, IOutputChannel, ILoggerService, IStorageService
├── Services/          — KeyboardOutput, SerialOutput, TCP, UDP, HTTP, Logger, Settings
├── Devices/
│   ├── Scanner/       — Barcode/QR scanner simulator
│   ├── VirtualKeyboard/
│   ├── PosPanel/      — Custom button panel
│   ├── SerialDevice/  — Serial COM port sender with simulation
│   ├── Scale/         — Weighing scale simulator
│   ├── RfidReader/    — RFID/NFC reader simulator
│   ├── MagstripeReader/ — Card swipe simulator
│   └── SequenceBuilder/ — Test sequence recorder/player
├── ViewModels/        — MVVM ViewModels for all pages
├── Views/             — Dashboard, Devices, Profiles, Logs, Settings
├── Helpers/           — RelayCommand, AsyncRelayCommand, ViewModelBase
└── Profiles/          — Default JSON profiles
```

---

## Architecture

**MVVM Pattern** — ViewModels handle logic, Views handle UI, Services handle I/O.

**Device → Router → Channel** — Every device creates a `DeviceAction`, the `InputRouter` routes it to the correct `IOutputChannel`, and the channel sends it to the target.

**Modular Design** — Adding a new device or output channel requires:
1. Create a class implementing `IDeviceModule` or `IOutputChannel`
2. Register it in `ServiceLocator`
3. Create a ViewModel + View
4. Add a DataTemplate in MainWindow

---

## Future Roadmap

- Receipt Printer Tester (ESC/POS commands)
- Cash Drawer Trigger
- Gamepad / Controller Simulator
- WebSocket Output Channel
- MQTT Output Channel
- Plugin System
- Macro Recorder
- Multi-device Simulation
- Installer / Auto-update

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the **Apache License 2.0** — see the [LICENSE](LICENSE) file for details.

---

## Author

**x1n-Q** — [GitHub](https://github.com/x1n-Q)

---

*DeviceMocker — Test your software without buying hardware.*
