# Contributing

Contributions are welcome! Here's how to get started.

## Development Setup

1. Fork the repository on GitHub
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/DeviceMocker.git
   ```
3. Open in Visual Studio 2022 or VS Code
4. Build: `dotnet build`
5. Run: `dotnet run`

## Guidelines

- Follow **MVVM pattern** — no business logic in code-behind
- Use **services** for I/O operations (sending, logging, storage)
- Use **async/await** for anything that might block the UI
- Keep each class focused on **one responsibility**
- Use the existing **style system** (StaticResource brushes and styles)
- Test your changes before submitting

## Pull Request Process

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes
3. Build and verify: `dotnet build`
4. Commit: `git commit -m 'Add my feature'`
5. Push: `git push origin feature/my-feature`
6. Open a Pull Request on GitHub

## Ideas for Contributions

- New device simulators (receipt printer, cash drawer, gamepad)
- New output channels (WebSocket, MQTT, named pipe)
- UI improvements and themes
- Test coverage
- Documentation improvements
- Bug fixes

## License

By contributing, you agree that your contributions will be licensed under the Apache License 2.0.
