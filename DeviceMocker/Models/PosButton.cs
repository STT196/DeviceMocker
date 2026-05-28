namespace DeviceMocker.Models
{
    public class PosButton
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public ActionType ActionType { get; set; } = ActionType.Key;
        public string Value { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public int DelayMs { get; set; } = 0;
    }
}
