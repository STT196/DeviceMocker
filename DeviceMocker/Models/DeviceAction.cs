using System;
using System.Collections.Generic;

namespace DeviceMocker.Models
{
    public class DeviceAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; }
        public ActionType ActionType { get; set; } = ActionType.Text;
        public OutputChannelType OutputChannelType { get; set; } = OutputChannelType.Keyboard;
        public string Payload { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public int DelayPerCharacterMs { get; set; } = 10;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
