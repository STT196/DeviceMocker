using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class KeyboardOutputService : IOutputChannel
    {
        public string Id => "keyboard-output";
        public string Name => "Keyboard Wedge";
        public OutputChannelType ChannelType => OutputChannelType.Keyboard;

        #region Native Interop

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        // Virtual key codes
        private const ushort VK_RETURN = 0x0D;
        private const ushort VK_TAB = 0x09;
        private const ushort VK_ESCAPE = 0x1B;
        private const ushort VK_BACK = 0x08;
        private const ushort VK_SPACE = 0x20;
        private const ushort VK_DELETE = 0x2E;
        private const ushort VK_LEFT = 0x25;
        private const ushort VK_UP = 0x26;
        private const ushort VK_RIGHT = 0x27;
        private const ushort VK_DOWN = 0x28;
        private const ushort VK_CONTROL = 0xA2;
        private const ushort VK_SHIFT = 0xA0;
        private const ushort VK_MENU = 0xA4; // Alt
        private const ushort VK_F1 = 0x70;

        #endregion

        private static readonly Dictionary<string, ushort> SpecialKeyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Enter", VK_RETURN },
            { "Return", VK_RETURN },
            { "Tab", VK_TAB },
            { "Escape", VK_ESCAPE },
            { "Esc", VK_ESCAPE },
            { "Backspace", VK_BACK },
            { "Space", VK_SPACE },
            { "Delete", VK_DELETE },
            { "Del", VK_DELETE },
            { "Left", VK_LEFT },
            { "Up", VK_UP },
            { "Right", VK_RIGHT },
            { "Down", VK_DOWN },
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
            { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
            { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
        };

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            try
            {
                var fullPayload = $"{action.Prefix}{action.Payload}";

                switch (action.ActionType)
                {
                    case ActionType.Text:
                        await SendTextAsync(fullPayload, action.DelayPerCharacterMs, cancellationToken);
                        await SendSuffixAsync(action.Suffix, cancellationToken);
                        break;

                    case ActionType.Key:
                        SendSpecialKey(action.Payload);
                        break;

                    case ActionType.Shortcut:
                        SendShortcut(action.Payload);
                        break;

                    case ActionType.Sequence:
                        // For sequence, payload contains semicolon-separated commands
                        var parts = action.Payload.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var trimmed = part.Trim();
                            if (SpecialKeyMap.ContainsKey(trimmed))
                                SendSpecialKey(trimmed);
                            else if (trimmed.Contains('+'))
                                SendShortcut(trimmed);
                            else
                                await SendTextAsync(trimmed, action.DelayPerCharacterMs, cancellationToken);

                            if (action.DelayPerCharacterMs > 0)
                                await Task.Delay(action.DelayPerCharacterMs, cancellationToken);
                        }
                        break;
                }

                return OutputResult.Ok();
            }
            catch (OperationCanceledException)
            {
                return OutputResult.Fail("Send operation was cancelled.");
            }
            catch (Exception ex)
            {
                return OutputResult.Fail($"Keyboard output error: {ex.Message}");
            }
        }

        private async Task SendTextAsync(string text, int delayMs, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(text)) return;

            foreach (char c in text)
            {
                ct.ThrowIfCancellationRequested();
                SendUnicodeChar(c);
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);
            }
        }

        private async Task SendSuffixAsync(string suffix, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(suffix) || suffix.Equals("None", StringComparison.OrdinalIgnoreCase))
                return;

            switch (suffix.ToUpperInvariant())
            {
                case "ENTER":
                    SendSpecialKey("Enter");
                    break;
                case "TAB":
                    SendSpecialKey("Tab");
                    break;
                case "CR":
                    SendUnicodeChar('\r');
                    break;
                case "LF":
                    SendUnicodeChar('\n');
                    break;
                case "CRLF":
                    SendUnicodeChar('\r');
                    SendUnicodeChar('\n');
                    break;
                default:
                    if (SpecialKeyMap.ContainsKey(suffix))
                        SendSpecialKey(suffix);
                    break;
            }
            await Task.CompletedTask;
        }

        private void SendUnicodeChar(char c)
        {
            var inputs = new INPUT[2];

            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        }

        private void SendSpecialKey(string keyName)
        {
            if (!SpecialKeyMap.TryGetValue(keyName, out var vk))
                return;

            var inputs = new INPUT[2];

            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT { wVk = vk, dwFlags = 0 }
                }
            };

            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        }

        public void SendShortcut(string shortcut)
        {
            // Parse shortcut like "Ctrl+C", "Ctrl+Shift+V", "Alt+F4"
            var keys = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var modifiers = new List<ushort>();
            ushort mainKey = 0;

            foreach (var key in keys)
            {
                switch (key.ToUpperInvariant())
                {
                    case "CTRL":
                    case "CONTROL":
                        modifiers.Add(VK_CONTROL);
                        break;
                    case "SHIFT":
                        modifiers.Add(VK_SHIFT);
                        break;
                    case "ALT":
                        modifiers.Add(VK_MENU);
                        break;
                    default:
                        if (SpecialKeyMap.TryGetValue(key, out var specialVk))
                            mainKey = specialVk;
                        else if (key.Length == 1)
                            mainKey = (ushort)char.ToUpper(key[0]);
                        break;
                }
            }

            if (mainKey == 0) return;

            // Build input array: press modifiers, press key, release key, release modifiers
            var inputList = new List<INPUT>();

            foreach (var mod in modifiers)
            {
                inputList.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION { ki = new KEYBDINPUT { wVk = mod, dwFlags = 0 } }
                });
            }

            inputList.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION { ki = new KEYBDINPUT { wVk = mainKey, dwFlags = 0 } }
            });

            inputList.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION { ki = new KEYBDINPUT { wVk = mainKey, dwFlags = KEYEVENTF_KEYUP } }
            });

            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                inputList.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new INPUTUNION { ki = new KEYBDINPUT { wVk = modifiers[i], dwFlags = KEYEVENTF_KEYUP } }
                });
            }

            var inputs = inputList.ToArray();
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}
