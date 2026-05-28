# Installation

## Prerequisites

- **Windows 10 or 11**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (required to build)
- Visual Studio 2022 (optional, for development)

## Option 1: Clone and Run

```bash
git clone https://github.com/x1n-Q/DeviceMocker.git
cd DeviceMocker/DeviceMocker
dotnet run
```

## Option 2: Open in Visual Studio

1. Clone the repository
2. Open `DeviceMocker/DeviceMocker.sln` in Visual Studio 2022
3. Press **F5** to build and run

## Option 3: Build a Release

```bash
cd DeviceMocker/DeviceMocker
dotnet publish -c Release -r win-x64 --self-contained
```

The output will be in `bin/Release/net8.0-windows/win-x64/publish/`.

## Verify Installation

When the app opens, you should see:
- A dark-themed window with a sidebar (Dashboard, Devices, Profiles, Logs, Settings)
- The Dashboard showing the app name, version, and your GitHub link
- 8 device simulators available under Devices
