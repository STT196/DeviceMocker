# Architecture

## Design Principles

- **MVVM Pattern** — ViewModels handle logic, Views handle UI
- **Device ↔ Channel Separation** — Devices don't know about output channels
- **Service Locator** — Central registry for all services, devices, and channels
- **Interface-Driven** — `IDeviceModule`, `IOutputChannel`, `ILoggerService`, `IStorageService`

## Data Flow

```
User clicks "Send"
  → ViewModel creates DeviceAction
  → Device.SendAsync(action)
  → InputRouter.RouteAsync(action)
  → OutputChannelManager.GetChannel(action.OutputChannelType)
  → IOutputChannel.SendAsync(action)
  → LoggerService.Log(result)
```

## Key Interfaces

### IDeviceModule
```csharp
public interface IDeviceModule
{
    string Id { get; }
    string Name { get; }
    DeviceType DeviceType { get; }
    Task<OutputResult> SendAsync(DeviceAction action, CancellationToken ct);
}
```

### IOutputChannel
```csharp
public interface IOutputChannel
{
    string Id { get; }
    string Name { get; }
    OutputChannelType ChannelType { get; }
    Task<OutputResult> SendAsync(DeviceAction action, CancellationToken ct);
}
```

## Project Structure

```
Core/           — ServiceLocator, InputRouter, OutputChannelManager, DeviceManager
Models/         — DeviceAction, DeviceProfile, DeviceLog, OutputResult, enums
Interfaces/     — IDeviceModule, IOutputChannel, ILoggerService, IStorageService
Services/       — Keyboard, Serial, TCP, UDP, HTTP output + Logger, Settings, Storage
Devices/        — 8 device modules (each: Device.cs, ViewModel.cs, View.xaml)
ViewModels/     — Page ViewModels (Dashboard, Devices, Profiles, Logs, Settings)
Views/          — Page Views
Helpers/        — RelayCommand, AsyncRelayCommand, ViewModelBase
Profiles/       — Default JSON profiles
```

## Adding New Components

See:
- [Adding a New Device](Adding-a-New-Device)
- [Adding a New Output Channel](Adding-a-New-Output-Channel)
