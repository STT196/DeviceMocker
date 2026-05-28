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
    }
}
