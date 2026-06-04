namespace DeviceMocker.Models
{
    public class AppSettings
    {
        public int DefaultDelayPerCharacterMs { get; set; } = 10;
        public string DefaultSuffix { get; set; } = "Enter";
        public OutputChannelType DefaultOutputChannel { get; set; } = OutputChannelType.Keyboard;
        public string Theme { get; set; } = "Dark";
        public bool LogToFile { get; set; } = false;
        public int MaxLogEntries { get; set; } = 1000;
        public int CountdownSeconds { get; set; } = 3;
        public bool EmulatorAutoStart { get; set; }
        public string EmulatorAutoStartProfileId { get; set; } = string.Empty;
        public EmulatorTransportType DefaultEmulatorTransport { get; set; } = EmulatorTransportType.Tcp;
        public string DefaultEmulatorSerialPort { get; set; } = string.Empty;
        public int DefaultEmulatorBaudRate { get; set; } = 9600;
        public string DefaultEmulatorTcpHost { get; set; } = "127.0.0.1";
        public int DefaultEmulatorTcpPort { get; set; } = 9100;
        public int DefaultEmulatorHttpPort { get; set; } = 8088;
        public string DefaultEmulatorHttpRoute { get; set; } = "/emulator";
        public EmulatorLogVerbosity EmulatorLogVerbosity { get; set; } = EmulatorLogVerbosity.ParsedAndRaw;
    }
}
