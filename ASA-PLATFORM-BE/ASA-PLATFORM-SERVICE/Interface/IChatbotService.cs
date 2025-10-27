using ASA_PLATFORM_SERVICE.DTOs;

namespace ASA_PLATFORM_SERVICE.Interface
{
    public interface IChatbotService
    {
        Task<ChatbotResponseDto> ProcessQuestionAsync(ChatbotRequestDto request);
    }
}

