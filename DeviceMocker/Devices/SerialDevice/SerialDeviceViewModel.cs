using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.SerialDevice
{
    public class SerialDeviceViewModel : ViewModelBase
    {
        private string _payload = string.Empty;
        private string _selectedPort = string.Empty;
        private int _baudRate = 9600;
        private string _selectedLineEnding = "None";
        private string _statusMessage = string.Empty;
        private bool _isSending;
        private bool _isConnected;
        private bool _isSimulationMode = true;
        private string _terminalOutput = string.Empty;
        private string _selectedSimDevice = "Echo";
        private readonly StringBuilder _terminalBuffer = new();

        public ObservableCollection<string> AvailablePorts { get; } = new();

        public string Payload
        {
            get => _payload;
            set => SetProperty(ref _payload, value);
        }

        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        public string SelectedLineEnding
        {
            get => _selectedLineEnding;
            set => SetProperty(ref _selectedLineEnding, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSending
        {
            get => _isSending;
            set => SetProperty(ref _isSending, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool IsSimulationMode
        {
            get => _isSimulationMode;
            set
            {
                if (SetProperty(ref _isSimulationMode, value))
                {
                    if (value)
                    {
                        Disconnect();
                        IsConnected = false;
                        StatusMessage = "Simulation mode — no hardware needed.";
                    }
                    else
                    {
                        StatusMessage = "Hardware mode — select a COM port.";
                        RefreshPorts();
                    }
                }
            }
        }

        public string TerminalOutput
        {
            get => _terminalOutput;
            set => SetProperty(ref _terminalOutput, value);
        }

        public string SelectedSimDevice
        {
            get => _selectedSimDevice;
            set => SetProperty(ref _selectedSimDevice, value);
        }

        public string[] LineEndingOptions { get; } = { "None", "CR", "LF", "CRLF" };
        public int[] BaudRateOptions { get; } = { 300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        public string[] SimDeviceOptions { get; } =
        {
            "Echo",
            "Weighing Scale",
            "Barcode Scanner",
            "Temperature Sensor",
            "Access Control"
        };

        public ICommand RefreshPortsCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ClearTerminalCommand { get; }
        public ICommand PresetCommand { get; }

        public SerialDeviceViewModel()
        {
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            SendCommand = new AsyncRelayCommand(SendAsync, () => !IsSending && !string.IsNullOrEmpty(Payload));
            ConnectCommand = new RelayCommand(Connect, () => !IsSimulationMode && !IsConnected && !string.IsNullOrEmpty(SelectedPort));
            DisconnectCommand = new RelayCommand(Disconnect, () => !IsSimulationMode && IsConnected);
            ClearTerminalCommand = new RelayCommand(ClearTerminal);
            PresetCommand = new AsyncRelayCommand(SendPresetAsync);

            AppendTerminal("SYS", "Serial Text Sender ready.");
            AppendTerminal("SYS", "Simulation mode active — select a virtual device and send data.");
            StatusMessage = "Simulation mode — no hardware needed.";
        }

        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            foreach (var port in Services.SerialOutputService.GetAvailablePorts())
                AvailablePorts.Add(port);

            StatusMessage = AvailablePorts.Count == 0
                ? "No COM ports found. Use Simulation Mode instead."
                : $"Found {AvailablePorts.Count} port(s).";
        }

        private void Connect()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    StatusMessage = "Please select a COM port.";
                    return;
                }

                var serial = ServiceLocator.SerialOutput;
                serial.PortName = SelectedPort;
                serial.BaudRate = BaudRate;
                serial.Open();
                IsConnected = true;
                StatusMessage = $"Connected to {SelectedPort} at {BaudRate} baud.";
                AppendTerminal("SYS", $"Connected to {SelectedPort} @ {BaudRate} baud");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection error: {ex.Message}";
                IsConnected = false;
            }
        }

        private void Disconnect()
        {
            try
            {
                if (ServiceLocator.SerialOutput.IsOpen)
                {
                    ServiceLocator.SerialOutput.Close();
                    AppendTerminal("SYS", "Disconnected.");
                }
                IsConnected = false;
                StatusMessage = "Disconnected.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Disconnect error: {ex.Message}";
            }
        }

        private async Task SendAsync()
        {
            if (string.IsNullOrEmpty(Payload))
            {
                StatusMessage = "Please enter a payload.";
                return;
            }

            IsSending = true;

            try
            {
                var lineEnding = GetLineEndingDisplay(SelectedLineEnding);
                var fullPayload = Payload + GetLineEndingChars(SelectedLineEnding);

                if (IsSimulationMode)
                {
                    await SimulationSendAsync(Payload, lineEnding);
                }
                else
                {
                    var serial = ServiceLocator.SerialOutput;
                    serial.PortName = SelectedPort;
                    serial.BaudRate = BaudRate;

                    var action = new DeviceAction
                    {
                        DeviceId = "serial-device",
                        DeviceName = "Serial Text Sender",
                        DeviceType = DeviceType.SerialDevice,
                        ActionType = ActionType.Text,
                        OutputChannelType = OutputChannelType.Serial,
                        Payload = Payload,
                        Suffix = SelectedLineEnding
                    };

                    var result = await ServiceLocator.SerialDeviceSimulator.SendAsync(action);
                    if (result.Success)
                    {
                        AppendTerminal("TX", $"{Payload}{lineEnding}");
                        StatusMessage = "Sent successfully!";
                    }
                    else
                    {
                        StatusMessage = $"Error: {result.ErrorMessage}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSending = false;
            }
        }

        private async Task SimulationSendAsync(string data, string lineEndingDisplay)
        {
            // Show what was sent
            AppendTerminal("TX", $"{data}{lineEndingDisplay}");
            AppendTerminal("HEX", $"  {ToHexString(data + GetLineEndingChars(SelectedLineEnding))}");

            // Log to app logs
            ServiceLocator.Logger.Log(new DeviceLog
            {
                DeviceName = "Serial Text Sender (Sim)",
                DeviceType = DeviceType.SerialDevice,
                OutputChannelType = OutputChannelType.Serial,
                Payload = data,
                Success = true
            });

            // Simulate processing delay
            await Task.Delay(150);

            // Generate simulated response
            var response = GenerateSimResponse(data);
            AppendTerminal("RX", response);
            AppendTerminal("HEX", $"  {ToHexString(response)}");

            StatusMessage = "Sent and received simulated response.";
        }

        private string GenerateSimResponse(string sentData)
        {
            return SelectedSimDevice switch
            {
                "Echo" => sentData,

                "Weighing Scale" => sentData.Trim().ToUpper() switch
                {
                    "W" or "WEIGHT" or "READ" => $"ST,GS,+  {new Random().Next(1, 500):D3}.{new Random().Next(0, 99):D2}  kg",
                    "Z" or "ZERO" or "TARE" => "ST,GS,+    0.00  kg",
                    "S" or "STATUS" => "OK,STABLE",
                    _ => $"ST,GS,+  {new Random().Next(1, 999):D3}.{new Random().Next(0, 99):D2}  kg"
                },

                "Barcode Scanner" => sentData.Trim().ToUpper() switch
                {
                    "SCAN" or "READ" or "TRIGGER" => $"EAN13:{new Random().Next(100000, 999999):D6}{new Random().Next(100000, 999999):D6}{new Random().Next(0, 9)}",
                    "STATUS" => "SCANNER:READY,LASER:ON",
                    "DISABLE" => "ACK:DISABLED",
                    "ENABLE" => "ACK:ENABLED",
                    _ => $"CODE128:{sentData.Trim()}"
                },

                "Temperature Sensor" => sentData.Trim().ToUpper() switch
                {
                    "T" or "TEMP" or "READ" => $"+{20 + new Random().Next(0, 15):D2}.{new Random().Next(0, 9)}C",
                    "H" or "HUMIDITY" => $"{40 + new Random().Next(0, 40)}%RH",
                    "STATUS" => "OK,SENSOR:ACTIVE,INTERVAL:1000ms",
                    _ => $"+{20 + new Random().Next(0, 15):D2}.{new Random().Next(0, 9)}C  {40 + new Random().Next(0, 40)}%RH"
                },

                "Access Control" => sentData.Trim().ToUpper() switch
                {
                    "OPEN" or "UNLOCK" => "ACK:DOOR_UNLOCKED,DURATION:5s",
                    "CLOSE" or "LOCK" => "ACK:DOOR_LOCKED",
                    "STATUS" => "DOOR:CLOSED,LOCK:ENGAGED,ALARM:OFF",
                    _ when sentData.Trim().Length >= 8 => $"CARD:{sentData.Trim()},ACCESS:GRANTED,USER:Employee #{new Random().Next(100, 999)}",
                    _ => "NAK:UNKNOWN_COMMAND"
                },

                _ => sentData
            };
        }

        private async Task SendPresetAsync(object? parameter)
        {
            if (parameter is not string preset) return;

            Payload = preset;
            await SendAsync();
        }

        private void ClearTerminal()
        {
            _terminalBuffer.Clear();
            TerminalOutput = string.Empty;
            AppendTerminal("SYS", "Terminal cleared.");
        }

        private void AppendTerminal(string tag, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var line = $"[{timestamp}] [{tag}] {message}";
            _terminalBuffer.AppendLine(line);
            TerminalOutput = _terminalBuffer.ToString();
        }

        private static string ToHexString(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                sb.Append($"{(int)c:X2} ");
            }
            return sb.ToString().TrimEnd();
        }

        private static string GetLineEndingChars(string suffix)
        {
            return suffix?.ToUpperInvariant() switch
            {
                "CR" => "\r",
                "LF" => "\n",
                "CRLF" => "\r\n",
                _ => string.Empty
            };
        }

        private static string GetLineEndingDisplay(string suffix)
        {
            return suffix?.ToUpperInvariant() switch
            {
                "CR" => "<CR>",
                "LF" => "<LF>",
                "CRLF" => "<CR><LF>",
                _ => string.Empty
            };
        }
    }
}
