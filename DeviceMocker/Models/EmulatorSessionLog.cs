using System;

namespace DeviceMocker.Models
{
    public class EmulatorSessionLog
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public EmulatorSessionLogKind Kind { get; set; } = EmulatorSessionLogKind.Info;
        public EmulatorTransportType Transport { get; set; } = EmulatorTransportType.Tcp;
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DataHex { get; set; } = string.Empty;
    }
}
