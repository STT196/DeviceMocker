using System;
using System.Collections.Generic;

namespace DeviceMocker.Models
{
    public class DeviceProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; }
        public string Description { get; set; } = string.Empty;
        public OutputChannelType DefaultOutputChannel { get; set; } = OutputChannelType.Keyboard;
        public string DefaultPrefix { get; set; } = string.Empty;
        public string DefaultSuffix { get; set; } = string.Empty;
        public int DelayPerCharacterMs { get; set; } = 10;
        public List<PosButton> Buttons { get; set; } = new();
        public Dictionary<string, string> Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
