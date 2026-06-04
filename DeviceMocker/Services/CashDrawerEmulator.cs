using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class CashDrawerEmulator : IEmulatorModule
    {
        private readonly List<byte> _pendingBytes = new();
        private string _sessionLabel = "cash-drawer";

        public string Id => "cash-drawer-emulator";
        public string Name => "Cash Drawer Emulator";
        public string ReceiptPreview => string.Empty;
        public bool IsDrawerOpen { get; private set; }

        public event Action<EmulatorSessionLog>? LogProduced;
        public event Action? StateChanged;

        public void Start(EmulatorProfileSettings settings)
        {
            _pendingBytes.Clear();
            _sessionLabel = settings.DeviceFamily == EmulatorDeviceFamily.CashDrawer ? "drawer-standalone" : "printer-linked";
            IsDrawerOpen = false;
            EmitInfo("Cash drawer emulator ready. Drawer reset to closed.");
            StateChanged?.Invoke();
        }

        public void Stop()
        {
            _pendingBytes.Clear();
            IsDrawerOpen = false;
            EmitInfo("Cash drawer emulator stopped.");
            StateChanged?.Invoke();
        }

        public Task HandleBytesAsync(byte[] bytes, CancellationToken cancellationToken = default)
        {
            _pendingBytes.AddRange(bytes);
            ParsePendingAsciiCommands();
            return Task.CompletedTask;
        }

        public void OpenFromPrinterKick(string detail)
        {
            IsDrawerOpen = true;
            EmitLog(EmulatorSessionLogKind.Drawer, $"Drawer opened by printer kick ({detail}).");
            StateChanged?.Invoke();
        }

        public void OpenManual(string detail)
        {
            IsDrawerOpen = true;
            EmitLog(EmulatorSessionLogKind.Drawer, detail);
            StateChanged?.Invoke();
        }

        public void MarkClosed(string reason)
        {
            IsDrawerOpen = false;
            EmitLog(EmulatorSessionLogKind.Drawer, reason);
            StateChanged?.Invoke();
        }

        private void ParsePendingAsciiCommands()
        {
            var text = Encoding.ASCII.GetString(_pendingBytes.ToArray());
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            if (!text.EndsWith("\n", StringComparison.Ordinal) && !text.EndsWith("\r", StringComparison.Ordinal))
            {
                _pendingBytes.Clear();
                _pendingBytes.AddRange(Encoding.ASCII.GetBytes(lines[^1]));
            }
            else
            {
                _pendingBytes.Clear();
            }

            var completedCount = text.EndsWith("\n", StringComparison.Ordinal) || text.EndsWith("\r", StringComparison.Ordinal)
                ? lines.Length
                : lines.Length - 1;

            for (int i = 0; i < completedCount; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.Contains(EscPosPrinterEmulator.ResetDrawerTestCommand, StringComparison.OrdinalIgnoreCase))
                {
                    MarkClosed("Standalone reset command received from test client.");
                    continue;
                }

                var command = line.ToUpperInvariant();

                switch (command)
                {
                    case "OPEN_DRAWER":
                    case "NO_SALE":
                        OpenManual($"Standalone command received: {command}.");
                        break;
                    case "STATUS":
                        EmitLog(EmulatorSessionLogKind.Parsed, $"STATUS -> {(IsDrawerOpen ? "OPEN" : "CLOSED")}");
                        break;
                    case "CLOSE":
                    case "MARK_CLOSED":
                        MarkClosed($"Standalone command received: {command}.");
                        break;
                    default:
                        EmitLog(EmulatorSessionLogKind.Warning, $"Unknown drawer command: {command}");
                        break;
                }
            }
        }

        private void EmitInfo(string message) => EmitLog(EmulatorSessionLogKind.Info, message);

        private void EmitLog(EmulatorSessionLogKind kind, string message)
        {
            LogProduced?.Invoke(new EmulatorSessionLog
            {
                Kind = kind,
                Message = message,
                SessionId = _sessionLabel
            });
        }
    }
}
