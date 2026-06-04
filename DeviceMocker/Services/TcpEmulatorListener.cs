using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public sealed class TcpEmulatorListener : IDisposable
    {
        private readonly ConcurrentDictionary<string, string> _activeSessions = new();
        private Func<byte[], string, CancellationToken, Task>? _onBytesReceived;
        private CancellationTokenSource? _cancellationTokenSource;
        private System.Net.Sockets.TcpListener? _listener;

        public event Action<int, string>? SessionChanged;

        public bool IsRunning => _listener != null;

        public Task StartAsync(EmulatorEndpointConfig config, Func<byte[], string, CancellationToken, Task> onBytesReceived, CancellationToken cancellationToken)
        {
            Stop();

            if (config.TcpPort <= 0)
                throw new InvalidOperationException("TCP listener requires a valid port.");

            _onBytesReceived = onBytesReceived;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var address = ResolveAddress(config.TcpHost);
            _listener = new System.Net.Sockets.TcpListener(address, config.TcpPort);
            _listener.Start();

            _ = AcceptLoopAsync(_cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Stop();
            return Task.CompletedTask;
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _listener != null)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "tcp-client";
            var sessionId = Guid.NewGuid().ToString("N")[..8];

            _activeSessions[sessionId] = endpoint;
            SessionChanged?.Invoke(_activeSessions.Count, $"{_activeSessions.Count} active TCP session(s). Last: {endpoint}");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[4096];
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var read = await stream.ReadAsync(buffer, cancellationToken);
                        if (read <= 0)
                            break;

                        if (_onBytesReceived != null)
                        {
                            var payload = new byte[read];
                            Array.Copy(buffer, payload, read);
                            await _onBytesReceived(payload, endpoint, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
            finally
            {
                _activeSessions.TryRemove(sessionId, out _);
                var summary = _activeSessions.IsEmpty
                    ? "No active TCP sessions."
                    : $"{_activeSessions.Count} active TCP session(s).";
                SessionChanged?.Invoke(_activeSessions.Count, summary);
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _listener?.Stop();
            _listener = null;
            _activeSessions.Clear();
            SessionChanged?.Invoke(0, "No active TCP sessions.");
        }

        private static IPAddress ResolveAddress(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || host == "*" || host == "0.0.0.0")
                return IPAddress.Any;

            return IPAddress.TryParse(host, out var address) ? address : IPAddress.Loopback;
        }
    }
}
