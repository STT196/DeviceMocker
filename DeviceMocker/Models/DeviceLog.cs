using System;

namespace DeviceMocker.Models
{
    public class DeviceLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string DeviceName { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; }
        public OutputChannelType OutputChannelType { get; set; }
        public string Payload { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
