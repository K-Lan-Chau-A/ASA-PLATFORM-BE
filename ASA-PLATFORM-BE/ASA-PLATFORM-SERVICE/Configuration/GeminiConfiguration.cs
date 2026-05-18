namespace ASA_PLATFORM_SERVICE.Configuration
{
    public class GeminiConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
        public int MaxTokens { get; set; } = 2048;
        public double Temperature { get; set; } = 0.7;
        public int TimeoutSeconds { get; set; } = 30;
    }
}

