namespace DeviceMocker.Models
{
    public class EmulatorEndpointConfig
    {
        public EmulatorTransportType Transport { get; set; } = EmulatorTransportType.Tcp;
        public string SerialPortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 9600;
        public string TcpHost { get; set; } = "127.0.0.1";
        public int TcpPort { get; set; } = 9100;
        public int HttpPort { get; set; } = 8088;
        public string HttpRoute { get; set; } = "/emulator";
        public bool AutoStart { get; set; }
        public string EncodingMode { get; set; } = "RawBytes";

        public EmulatorEndpointConfig Clone()
        {
            return new EmulatorEndpointConfig
            {
                Transport = Transport,
                SerialPortName = SerialPortName,
                BaudRate = BaudRate,
                TcpHost = TcpHost,
                TcpPort = TcpPort,
                HttpPort = HttpPort,
                HttpRoute = HttpRoute,
                AutoStart = AutoStart,
                EncodingMode = EncodingMode
            };
        }
    }
}
