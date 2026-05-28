# Contributing to DeviceMocker

Thank you for your interest in contributing to DeviceMocker! This document provides guidelines and information for contributors.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How to Report Bugs](#how-to-report-bugs)
- [How to Suggest Features](#how-to-suggest-features)
- [Development Setup](#development-setup)
- [Code Style Guidelines](#code-style-guidelines)
- [Adding New Features](#adding-new-features)
- [Pull Request Process](#pull-request-process)
- [Commit Message Format](#commit-message-format)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## How to Report Bugs

Before creating bug reports, please check existing issues to avoid duplicates.

### Bug Report Template

When filing an issue, include:

1. **Clear title** - Use a descriptive title like "Scanner batch mode crashes with count > 100"
2. **Steps to reproduce** - Detailed steps to reproduce the issue
3. **Expected behavior** - What you expected to happen
4. **Actual behavior** - What actually happened
5. **Environment** - OS version, .NET version, DeviceMocker version
6. **Screenshots** - If applicable
7. **Logs** - Check `%LOCALAPPDATA%/DeviceMocker/Logs/` for relevant logs

## How to Suggest Features

Feature suggestions are welcome! Please:

1. Check existing issues and discussions first
2. Use the feature request template
3. Explain the use case and why it's valuable
4. Describe which device/channel it relates to
5. Consider implementation complexity

## Development Setup

### Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code (optional)
- Git

### Getting Started

```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/DeviceMocker.git
cd DeviceMocker

# Build the project
cd DeviceMocker
dotnet build

# Run the application
dotnet run
```

### Project Structure

```
DeviceMocker/
├── Core/              # ServiceLocator, InputRouter, managers
├── Models/            # Data models and enums
├── Interfaces/        # Service interfaces
├── Services/          # Output channels and utilities
├── Devices/           # Device simulators (8 devices)
├── ViewModels/        # MVVM ViewModels
├── Views/             # XAML Views
├── Helpers/           # Commands and base classes
└── Profiles/          # Default JSON profiles
```

## Code Style Guidelines

### General Principles

- **MVVM Pattern** - No business logic in code-behind files
- **Async/Await** - Use for all I/O operations
- **Single Responsibility** - One class, one purpose
- **Interface-Driven** - Program to interfaces, not implementations

### Naming Conventions

- **Classes**: PascalCase (`ScannerDevice`, `KeyboardOutputService`)
- **Methods**: PascalCase (`SendAsync`, `RouteAsync`)
- **Properties**: PascalCase (`DeviceType`, `Payload`)
- **Private fields**: _camelCase (`_router`, `_logger`)
- **Constants**: PascalCase (`DefaultCountdownSeconds`)
- **Interfaces**: IPrefix (`IDeviceModule`, `IOutputChannel`)

### Code Organization

```csharp
// Good: Clear separation of concerns
public class ScannerDevice : IDeviceModule
{
    private readonly InputRouter _router;
    
    public ScannerDevice(InputRouter router)
    {
        _router = router;
    }
    
    public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken ct)
    {
        action.DeviceId = Id;
        action.DeviceName = Name;
        action.DeviceType = DeviceType;
        return await _router.RouteAsync(action, ct);
    }
}
```

### XAML Guidelines

- Use `StaticResource` for brushes and styles
- Keep views declarative - no code-behind logic
- Use data binding for all dynamic content
- Follow existing spacing and indentation patterns

## Adding New Features

### Adding a New Device Module

1. **Create device folder**: `Devices/MyDevice/`
2. **Add enum value**: `Models/DeviceType.cs`
3. **Create device class**: Implement `IDeviceModule`
4. **Create ViewModel**: Extend `ViewModelBase`
5. **Create View**: XAML UserControl
6. **Register in ServiceLocator**: `DeviceManager.Register()`
7. **Add navigation**: `DevicesViewModel` switch case
8. **Add DataTemplate**: `MainWindow.xaml`
9. **Add device card**: `DevicesView.xaml`

See [Adding a New Device](wiki/Adding-a-New-Device.md) for detailed steps.

### Adding a New Output Channel

1. **Add enum value**: `Models/OutputChannelType.cs`
2. **Create service class**: Implement `IOutputChannel`
3. **Register in ServiceLocator**: `ChannelManager.Register()`

See [Adding a New Output Channel](wiki/Adding-a-New-Output-Channel.md) for detailed steps.

## Pull Request Process

### Before Submitting

1. **Create a feature branch** from `main`
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make your changes**
   - Follow code style guidelines
   - Add/update tests if applicable
   - Update documentation

3. **Build and test**
   ```bash
   dotnet build
   dotnet run  # Manual testing
   ```

4. **Commit with conventional commits**
   ```bash
   git commit -m "feat: add new device simulator"
   ```

5. **Push to your fork**
   ```bash
   git push origin feature/my-feature
   ```

6. **Open a Pull Request**
   - Use the PR template
   - Link related issues
   - Describe your changes clearly

### PR Checklist

- [ ] Code follows MVVM pattern
- [ ] No business logic in code-behind
- [ ] Async/await used for I/O operations
- [ ] Code builds without warnings
- [ ] Manual testing completed
- [ ] Documentation updated (if needed)
- [ ] Commit messages follow conventional commits

### Code Review

- All PRs require at least one review
- Address review comments promptly
- Be respectful and constructive
- Ask questions if requirements are unclear

## Commit Message Format

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, semicolons)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```
feat(scanner): add batch scan mode with configurable interval

fix(scale): correct weight format for CAS protocol

docs(wiki): add guide for adding new devices

refactor(router): simplify channel selection logic
```

## Questions?

Feel free to open an issue or start a discussion if you have questions about contributing!

Thank you for helping make DeviceMocker better! 🎉
