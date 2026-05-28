using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class TcpOutputService : IOutputChannel
    {
        public string Id => "tcp-output";
        public string Name => "TCP Client";
        public OutputChannelType ChannelType => OutputChannelType.TcpClient;

        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9000;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(Host) || Port <= 0)
                    return OutputResult.Fail("Invalid TCP host or port.");

                var payload = $"{action.Prefix}{action.Payload}{action.Suffix}";
                using var client = new TcpClient();
                await client.ConnectAsync(Host, Port, cancellationToken);
                var stream = client.GetStream();
                var data = Encoding.UTF8.GetBytes(payload);
                await stream.WriteAsync(data, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                return OutputResult.Ok();
            }
            catch (Exception ex)
            {
                return OutputResult.Fail($"TCP error: {ex.Message}");
            }
        }
    }
}
