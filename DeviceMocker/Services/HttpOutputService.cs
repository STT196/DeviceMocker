using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Services
{
    public class HttpOutputService : IOutputChannel
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        public string Id => "http-output";
        public string Name => "HTTP Webhook";
        public OutputChannelType ChannelType => OutputChannelType.HttpWebhook;

        public string Url { get; set; } = "http://localhost:8080/webhook";
        public string Method { get; set; } = "POST";

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(Url))
                    return OutputResult.Fail("No webhook URL configured.");

                var body = new
                {
                    deviceId = action.DeviceId,
                    deviceName = action.DeviceName,
                    deviceType = action.DeviceType.ToString(),
                    payload = $"{action.Prefix}{action.Payload}{action.Suffix}",
                    timestamp = DateTime.UtcNow.ToString("o"),
                    metadata = action.Metadata
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { WriteIndented = false });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                    response = await _httpClient.PutAsync(Url, content, cancellationToken);
                else
                    response = await _httpClient.PostAsync(Url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return OutputResult.Ok();
                else
                    return OutputResult.Fail($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }
            catch (TaskCanceledException)
            {
                return OutputResult.Fail("HTTP request timed out.");
            }
            catch (Exception ex)
            {
                return OutputResult.Fail($"HTTP error: {ex.Message}");
            }
        }
    }
}
