using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class UdpOutputService : IOutputChannel
    {
        public string Id => "udp-output";
        public string Name => "UDP";
        public OutputChannelType ChannelType => OutputChannelType.Udp;

        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9001;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(Host) || Port <= 0)
                    return OutputResult.Fail("Invalid UDP host or port.");

                var payload = $"{action.Prefix}{action.Payload}{action.Suffix}";
                using var client = new UdpClient();
                var data = Encoding.UTF8.GetBytes(payload);
                await client.SendAsync(data, data.Length, Host, Port);
                return OutputResult.Ok();
            }
            catch (Exception ex)
            {
                return OutputResult.Fail($"UDP error: {ex.Message}");
            }
        }
    }
}
