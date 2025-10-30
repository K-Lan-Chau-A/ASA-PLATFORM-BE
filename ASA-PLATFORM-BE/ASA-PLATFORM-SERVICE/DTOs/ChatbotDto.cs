namespace ASA_PLATFORM_SERVICE.DTOs
{
    public class ChatbotRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public string? ConversationHistory { get; set; }
    }

    public class ChatbotResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsReadyToRegister { get; set; }
        public TrialRegistrationDto? RegistrationData { get; set; }
    }

    public class ProductInfoDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public int? RequestLimit { get; set; }
        public int? AccountLimit { get; set; }
        public TimeSpan? Duration { get; set; }
        public List<string> Features { get; set; } = new();
    }

    public class FeatureInfoDto
    {
        public long FeatureId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TrialRegistrationDto
    {
        public string ShopName { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Phonenumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}

