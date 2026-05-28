using DeviceMocker.Models;

namespace DeviceMocker.Devices.SequenceBuilder
{
    public class SequenceStep
    {
        public int Order { get; set; }
        public string Label { get; set; } = string.Empty;
        public ActionType ActionType { get; set; } = ActionType.Text;
        public string Payload { get; set; } = string.Empty;
        public string Suffix { get; set; } = "Enter";
        public int DelayAfterMs { get; set; } = 500;
    }
}
