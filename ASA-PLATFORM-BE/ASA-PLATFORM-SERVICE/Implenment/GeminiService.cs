using ASA_PLATFORM_SERVICE.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class GeminiService
    {
        private readonly GeminiConfiguration _config;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiService(
            IOptions<GeminiConfiguration> config,
            ILogger<GeminiService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _config = config.Value;
            _logger = logger;
            _apiKey = _config.ApiKey ?? throw new InvalidOperationException("Gemini API key not configured");
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            const int maxRetries = 3;
            const int delayMs = 2000; // 2 seconds

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Calling Gemini API (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = _config.Temperature,
                            maxOutputTokens = _config.MaxTokens
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Build API endpoint
                    var uri = $"https://generativelanguage.googleapis.com/v1beta/models/{_config.Model}:generateContent?key={_apiKey}";
                    
                    if (attempt == 1)
                    {
                        _logger.LogInformation("Calling Gemini API with endpoint: {Uri}", uri.Replace(_apiKey, "API_KEY_HIDDEN"));
                    }
                    
                    var response = await _httpClient.PostAsync(uri, content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                        
                        // Retry for 503 (Service Unavailable) or 429 (Too Many Requests)
                        if ((response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                             response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) &&
                            attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in {Delay}ms...", delayMs * attempt);
                            await Task.Delay(delayMs * attempt); // Exponential backoff
                            continue; // Go to next attempt
                        }
                        
                        throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {errorContent}");
                    }

                    // Success response processing
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (geminiResponse?.candidates?.Length > 0 && 
                        geminiResponse.candidates[0].content?.parts?.Length > 0)
                    {
                        var text = geminiResponse.candidates[0].content.parts[0].text ?? "Không thể tạo phản hồi.";
                        _logger.LogInformation("Successfully generated response from Gemini");
                        return text;
                    }

                    throw new Exception("Invalid response from Gemini API");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error on attempt {Attempt}", attempt);
                    if (attempt == maxRetries)
                    {
                        throw;
                    }
                    // Continue to retry
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on attempt {Attempt}", attempt);
                    if (attempt == maxRetries)
                    {
                        throw;
                    }
                    // Continue to retry
                }
            }

            // If all retries failed
            throw new Exception("Failed to get response from Gemini API after multiple attempts");
        }

        private class GeminiApiResponse
        {
            public GeminiCandidate[]? candidates { get; set; }
        }

        private class GeminiCandidate
        {
            public GeminiContent? content { get; set; }
        }

        private class GeminiContent
        {
            public GeminiPart[]? parts { get; set; }
        }

        private class GeminiPart
        {
            public string? text { get; set; }
        }
    }
}

