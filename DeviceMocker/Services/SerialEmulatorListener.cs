using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public sealed class SerialEmulatorListener : IDisposable
    {
        private SerialPort? _serialPort;
        private Func<byte[], string, CancellationToken, Task>? _onBytesReceived;
        private CancellationToken _cancellationToken;

        public bool IsRunning => _serialPort?.IsOpen == true;
        public string PortName { get; private set; } = string.Empty;

        public Task StartAsync(EmulatorEndpointConfig config, Func<byte[], string, CancellationToken, Task> onBytesReceived, CancellationToken cancellationToken)
        {
            Stop();

            if (string.IsNullOrWhiteSpace(config.SerialPortName))
                throw new InvalidOperationException("Serial listener requires a COM port.");

            _onBytesReceived = onBytesReceived;
            _cancellationToken = cancellationToken;
            PortName = config.SerialPortName;

            _serialPort = new SerialPort(config.SerialPortName, config.BaudRate)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _serialPort.DataReceived += SerialPortOnDataReceived;
            _serialPort.Open();

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Stop();
            return Task.CompletedTask;
        }

        private async void SerialPortOnDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || _onBytesReceived == null)
                return;

            try
            {
                var bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead <= 0)
                    return;

                var buffer = new byte[bytesToRead];
                var read = _serialPort.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;

                if (read != buffer.Length)
                    Array.Resize(ref buffer, read);

                await _onBytesReceived(buffer, $"Serial {PortName}", _cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private void Stop()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPortOnDataReceived;
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
    }
}
