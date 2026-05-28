using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class SerialOutputService : IOutputChannel, IDisposable
    {
        private SerialPort? _serialPort;

        public string Id => "serial-output";
        public string Name => "Serial COM Port";
        public OutputChannelType ChannelType => OutputChannelType.Serial;

        // Configuration
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public string LineEnding { get; set; } = "None";

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public void Open()
        {
            Close();
            _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits);
            _serialPort.Open();
        }

        public void Close()
        {
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
        }

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(PortName))
                    return OutputResult.Fail("No COM port selected.");

                var payload = $"{action.Prefix}{action.Payload}{GetLineEndingChars(action.Suffix)}";

                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    Open();
                }

                await Task.Run(() =>
                {
                    _serialPort!.Write(payload);
                }, cancellationToken);

                return OutputResult.Ok();
            }
            catch (Exception ex)
            {
                return OutputResult.Fail($"Serial output error: {ex.Message}");
            }
        }

        private string GetLineEndingChars(string suffix)
        {
            if (string.IsNullOrEmpty(suffix)) return string.Empty;

            return suffix.ToUpperInvariant() switch
            {
                "NONE" => string.Empty,
                "CR" => "\r",
                "LF" => "\n",
                "CRLF" => "\r\n",
                "ENTER" => "\r\n",
                "TAB" => "\t",
                _ => string.Empty
            };
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }
}
