using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class EscPosPrinterEmulator : IEmulatorModule
    {
        public const string ResetDrawerTestCommand = "<<DM_RESET_DRAWER>>";

        private readonly CashDrawerEmulator _cashDrawer;
        private readonly List<byte> _pendingBytes = new();
        private readonly StringBuilder _receiptBuilder = new();
        private readonly StringBuilder _currentLine = new();

        private int _alignmentMode;
        private bool _emphasis;
        private bool _renderPreview = true;
        private string _sessionLabel = "escpos";

        public string Id => "escpos-printer-emulator";
        public string Name => "ESC/POS Printer Emulator";
        public string ReceiptPreview => BuildPreviewText();
        public bool IsDrawerOpen => _cashDrawer.IsDrawerOpen;

        public event Action<EmulatorSessionLog>? LogProduced;
        public event Action? StateChanged;

        public EscPosPrinterEmulator(CashDrawerEmulator cashDrawer)
        {
            _cashDrawer = cashDrawer;
            _cashDrawer.LogProduced += log => LogProduced?.Invoke(log);
            _cashDrawer.StateChanged += () => StateChanged?.Invoke();
        }

        public void Start(EmulatorProfileSettings settings)
        {
            _pendingBytes.Clear();
            _receiptBuilder.Clear();
            _currentLine.Clear();
            _alignmentMode = 0;
            _emphasis = false;
            _renderPreview = settings.RenderReceiptPreview;
            _sessionLabel = settings.DeviceFamily == EmulatorDeviceFamily.ReceiptPrinter ? "receipt-printer" : "printer";
            _cashDrawer.MarkClosed("Printer emulator session started. Drawer reset to closed.");
            EmitParsed("ESC/POS printer emulator ready.");
            StateChanged?.Invoke();
        }

        public void Stop()
        {
            FlushCurrentLine(force: true);
            EmitParsed("ESC/POS printer emulator stopped.");
            StateChanged?.Invoke();
        }

        public Task HandleBytesAsync(byte[] bytes, CancellationToken cancellationToken = default)
        {
            _pendingBytes.AddRange(bytes);
            ParsePendingBytes();
            return Task.CompletedTask;
        }

        private void ParsePendingBytes()
        {
            var index = 0;

            while (index < _pendingBytes.Count)
            {
                var current = _pendingBytes[index];

                if (current == 0x1B)
                {
                    if (!TryParseEscCommand(ref index))
                        break;
                    continue;
                }

                if (current == 0x1D)
                {
                    if (!TryParseGsCommand(ref index))
                        break;
                    continue;
                }

                switch (current)
                {
                    case 0x0A:
                        FlushCurrentLine();
                        index++;
                        continue;
                    case 0x0D:
                        index++;
                        continue;
                    case 0x09:
                        _currentLine.Append("    ");
                        index++;
                        continue;
                }

                if (current >= 0x20 && current <= 0x7E)
                {
                    _currentLine.Append((char)current);
                    index++;
                    continue;
                }

                EmitWarning($"Unhandled byte 0x{current:X2}");
                index++;
            }

            if (index > 0)
                _pendingBytes.RemoveRange(0, index);

            StateChanged?.Invoke();
        }

        private bool TryParseEscCommand(ref int index)
        {
            if (index + 1 >= _pendingBytes.Count)
                return false;

            var command = _pendingBytes[index + 1];

            switch (command)
            {
                case 0x40:
                    if (index + 2 > _pendingBytes.Count)
                        return false;
                    _alignmentMode = 0;
                    _emphasis = false;
                    EmitParsed("ESC @ -> Initialize printer");
                    index += 2;
                    return true;

                case 0x70:
                    if (index + 4 >= _pendingBytes.Count)
                        return false;
                    var pin = _pendingBytes[index + 2];
                    var onTime = _pendingBytes[index + 3];
                    var offTime = _pendingBytes[index + 4];
                    _cashDrawer.OpenFromPrinterKick($"pin={pin}, on={onTime}, off={offTime}");
                    EmitParsed($"ESC p -> Drawer kick (pin={pin}, on={onTime}, off={offTime})");
                    index += 5;
                    return true;

                case 0x61:
                    if (index + 2 >= _pendingBytes.Count)
                        return false;
                    _alignmentMode = _pendingBytes[index + 2] switch
                    {
                        1 => 1,
                        2 => 2,
                        _ => 0
                    };
                    EmitParsed($"ESC a -> Alignment {(_alignmentMode == 1 ? "Center" : _alignmentMode == 2 ? "Right" : "Left")}");
                    index += 3;
                    return true;

                case 0x45:
                    if (index + 2 >= _pendingBytes.Count)
                        return false;
                    _emphasis = _pendingBytes[index + 2] != 0;
                    EmitParsed($"ESC E -> Emphasis {(_emphasis ? "On" : "Off")}");
                    index += 3;
                    return true;

                default:
                    EmitWarning($"Unknown ESC command 0x{command:X2}");
                    index += 2;
                    return true;
            }
        }

        private bool TryParseGsCommand(ref int index)
        {
            if (index + 1 >= _pendingBytes.Count)
                return false;

            var command = _pendingBytes[index + 1];

            switch (command)
            {
                case 0x56:
                    if (index + 2 >= _pendingBytes.Count)
                        return false;
                    var mode = _pendingBytes[index + 2];
                    EmitParsed($"GS V -> Cut paper (mode={mode})");
                    AppendRenderMarker("[CUT]");
                    index += 3;
                    return true;

                default:
                    EmitWarning($"Unknown GS command 0x{command:X2}");
                    index += 2;
                    return true;
            }
        }

        private void FlushCurrentLine(bool force = false)
        {
            if (_currentLine.Length == 0 && !force)
            {
                if (_renderPreview)
                    _receiptBuilder.AppendLine();
                return;
            }

            var line = _currentLine.ToString();
            if (string.Equals(line.Trim(), ResetDrawerTestCommand, StringComparison.Ordinal))
            {
                _cashDrawer.MarkClosed("Drawer reset by DeviceMocker test command.");
                EmitParsed("DeviceMocker test command -> Reset drawer state");
                _currentLine.Clear();
                return;
            }

            if (_emphasis && line.Length > 0)
                line = $"[B] {line}";

            if (_renderPreview)
                _receiptBuilder.AppendLine(ApplyAlignment(line));

            if (line.Length > 0)
                EmitRender($"Rendered line: {line}");

            _currentLine.Clear();
        }

        private void AppendRenderMarker(string marker)
        {
            FlushCurrentLine(force: true);
            if (_renderPreview)
                _receiptBuilder.AppendLine(marker);
            EmitRender(marker);
        }

        private string ApplyAlignment(string line)
        {
            const int width = 42;
            if (line.Length >= width)
                return line;

            return _alignmentMode switch
            {
                1 => line.PadLeft((width + line.Length) / 2),
                2 => line.PadLeft(width),
                _ => line
            };
        }

        private string BuildPreviewText()
        {
            if (_currentLine.Length == 0)
                return _receiptBuilder.ToString();

            var previewLines = new[] { _receiptBuilder.ToString().TrimEnd('\r', '\n'), ApplyAlignment(_currentLine.ToString()) }
                .Where(x => !string.IsNullOrEmpty(x));
            return string.Join(Environment.NewLine, previewLines);
        }

        private void EmitParsed(string message) => EmitLog(EmulatorSessionLogKind.Parsed, message);
        private void EmitRender(string message) => EmitLog(EmulatorSessionLogKind.Render, message);
        private void EmitWarning(string message) => EmitLog(EmulatorSessionLogKind.Warning, message);

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
