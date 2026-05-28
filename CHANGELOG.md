# Changelog

All notable changes to DeviceMocker will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-05-28

### Added

#### Device Simulators (8 devices)
- **Barcode/QR Scanner** - Simulate barcode scanning with keyboard wedge output
  - 20 sample barcodes (EAN-13, UPC-A, Code128, QR codes)
  - Random barcode generator with check digit calculation
  - Batch scan mode with configurable count and interval
  - Scan history with click-to-rescan
- **Virtual Keyboard** - Send individual keys and shortcuts
  - Full keyboard layout (letters, numbers, function keys F1-F12)
  - Special keys (Enter, Tab, Escape, Backspace, Delete)
  - Arrow keys and shortcuts (Ctrl+C, Ctrl+V, etc.)
- **Custom Button Panel** - POS-style configurable buttons
  - Predefined buttons (Cash, Card, Discount, Submit, etc.)
  - Custom button configuration
  - Color-coded button categories
- **Serial Text Sender** - Send text to COM ports
  - Built-in simulation mode with 5 virtual devices
  - Hardware mode for real COM port communication
  - Terminal view with TX/RX hex display
  - Configurable baud rate, parity, data bits, stop bits
- **Weighing Scale** - Simulate weight readings
  - Standard scale format (ST,GS protocol)
  - Multiple units (kg, lb, g, oz)
  - Quick weight presets and random generator
  - Tare function
- **RFID/NFC Reader** - Simulate card taps
  - 8 sample cards (MIFARE Classic, Ultralight, DESFire, NTAG)
  - Multiple UID formats (hex, decimal, with colons/spaces)
  - Random UID generator
  - Tap history
- **Magstripe Card Reader** - Simulate card swipes
  - Track 1, 2, and 3 data support
  - 6 sample cards (Visa, MasterCard, Amex, Gift, Loyalty, Employee)
  - Random track data generator
  - Swipe history
- **Test Sequence Builder** - Build and replay action sequences
  - Multi-step sequences with configurable delays
  - 3 presets (POS Login, Scan 3 Items, Form Fill)
  - Step types (Text, Key, Shortcut)
  - Sequence editor with add/remove/reorder

#### Output Channels (5 channels)
- **Keyboard Wedge** - Types into active window using Windows SendInput API
- **Serial (COM Port)** - Sends data to physical or virtual COM ports
- **TCP Client** - Sends data to TCP servers
- **UDP** - Sends UDP datagrams
- **HTTP Webhook** - POSTs JSON payload to any URL

#### Core Features
- **Profile Management**
  - Create, edit, duplicate, delete profiles
  - Import/export profiles as JSON
  - Default profiles included
- **Activity Logging**
  - All actions logged with timestamp, device, channel, payload, status
  - Log export to CSV and JSON
  - Searchable log viewer
- **Settings**
  - Dark/Light theme toggle
  - Configurable countdown seconds
  - Default output channel selection
- **Architecture**
  - MVVM pattern with clean separation of concerns
  - Device ↔ Channel separation via InputRouter
  - Interface-driven design (IDeviceModule, IOutputChannel)
  - Service Locator pattern for dependency management
  - Async/await for all I/O operations

#### User Interface
- Modern dark theme with accent colors
- Responsive layout with sidebar navigation
- Dashboard with quick actions and recent logs
- Device cards with hover effects
- Inline profile editor
- Countdown timer for delayed sends

### Technical Details
- Built with .NET 8 and WPF
- Windows-only (uses SendInput API and serial ports)
- No external dependencies for core functionality
- Modular architecture for easy extension

### License
- Apache License 2.0

[1.0.0]: https://github.com/x1n-Q/DeviceMocker/releases/tag/v1.0.0
