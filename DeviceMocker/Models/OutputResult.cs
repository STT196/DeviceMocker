namespace DeviceMocker.Models
{
    public class OutputResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static OutputResult Ok() => new() { Success = true };
        public static OutputResult Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
