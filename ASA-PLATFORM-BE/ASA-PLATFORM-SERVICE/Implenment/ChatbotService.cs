using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.Configuration;
using ASA_PLATFORM_SERVICE.DTOs;
using ASA_PLATFORM_SERVICE.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class ChatbotService : IChatbotService
    {
        private readonly GeminiService _geminiService;
        private readonly ProductRepo _productRepo;
        private readonly FeatureRepo _featureRepo;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(
            GeminiService geminiService,
            ProductRepo productRepo,
            FeatureRepo featureRepo,
            ILogger<ChatbotService> logger)
        {
            _geminiService = geminiService;
            _productRepo = productRepo;
            _featureRepo = featureRepo;
            _logger = logger;
        }

        public async Task<ChatbotResponseDto> ProcessQuestionAsync(ChatbotRequestDto request)
        {
            try
            {
                // Lấy thông tin sản phẩm và tính năng từ DB
                var products = await GetProductsInfoAsync();
                var features = await GetFeaturesInfoAsync();

                // Tạo context cho AI
                var context = BuildContext(products, features);

                // Tạo prompt với context và conversation history
                var prompt = BuildPrompt(request.Question, context, request.ConversationHistory);

                // Gọi Gemini API
                var response = await _geminiService.GenerateResponseAsync(prompt);

                // Parse registration data từ response
                var registrationData = ParseRegistrationData(response, request.Question, request.ConversationHistory);

                return new ChatbotResponseDto
                {
                    Answer = response,
                    IsSuccess = true,
                    IsReadyToRegister = registrationData != null,
                    RegistrationData = registrationData,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot question");
                return new ChatbotResponseDto
                {
                    Answer = "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này. Vui lòng thử lại sau.",
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<List<ProductInfoDto>> GetProductsInfoAsync()
        {
            var products = await _productRepo.GetFiltered(new Product { Status = 1 })
                .Include(p => p.Features)
                .ToListAsync();

            return products.Select(p => new ProductInfoDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName ?? "Không có tên",
                Description = p.Description ?? "",
                Price = p.Price,
                RequestLimit = p.RequestLimit,
                AccountLimit = p.AccountLimit,
                Duration = p.Duration,
                Features = p.Features?.Select(f => f.FeatureName ?? "").ToList() ?? new List<string>()
            }).ToList();
        }

        private async Task<List<FeatureInfoDto>> GetFeaturesInfoAsync()
        {
            var features = await _featureRepo.GetFiltered(new Feature { IsActive = true }).ToListAsync();

            return features.Select(f => new FeatureInfoDto
            {
                FeatureId = f.FeatureId,
                FeatureName = f.FeatureName ?? "Không có tên",
                Description = f.Description ?? ""
            }).ToList();
        }

        private string BuildContext(List<ProductInfoDto> products, List<FeatureInfoDto> features)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== THÔNG TIN VỀ CÔNG TY ===");
            sb.AppendLine("Công ty Kì lân Châu Á");
            sb.AppendLine("- Đây là một công ty startup về công nghệ");
            sb.AppendLine("- Được thành lập bởi một nhóm học sinh từ Đại học FPT");
            sb.AppendLine("- Người sáng lập: Nguyễn Hà Mai Hân");
            sb.AppendLine("- Sản phẩm chính: AI Store Assistant");
            sb.AppendLine("- AI Store Assistant là phần mềm quản lý cửa hàng thông minh với AI hỗ trợ");
            sb.AppendLine();

            sb.AppendLine("=== DANH SÁCH SẢN PHẨM GÓI ===");
            foreach (var product in products)
            {
                sb.AppendLine($"- Gói: {product.ProductName}");
                sb.AppendLine($"  Mô tả: {product.Description}");
                sb.AppendLine($"  Giá: {product.Price:N0} VNĐ");
                if (product.RequestLimit.HasValue)
                    sb.AppendLine($"  Giới hạn request: {product.RequestLimit}");
                if (product.AccountLimit.HasValue)
                    sb.AppendLine($"  Giới hạn tài khoản: {product.AccountLimit}");
                if (product.Duration.HasValue)
                    sb.AppendLine($"  Thời hạn: {product.Duration.Value.TotalDays} ngày");
                if (product.Features.Any())
                {
                    sb.AppendLine($"  Tính năng: {string.Join(", ", product.Features)}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== DANH SÁCH TÍNH NĂNG CÓ SẴN ===");
            foreach (var feature in features)
            {
                sb.AppendLine($"- {feature.FeatureName}");
                sb.AppendLine($"  Mô tả: {feature.Description}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string BuildPrompt(string question, string context, string? conversationHistory)
        {
            var historySection = "";
            if (!string.IsNullOrWhiteSpace(conversationHistory))
            {
                historySection = $@"

=== LỊCH SỬ CUỘC HỘI THOẠI ===
{conversationHistory}

Hãy tham khảo lịch sử cuộc hội thoại trên để hiểu context và trả lời một cách tự nhiên, liên kết với các câu hỏi trước.
";
            }

            var prompt = $@"Bạn là Pony - trợ lý AI chuyên nghiệp và thân thiện của công ty Kì lân Châu Á. Bạn chuyên tư vấn về sản phẩm AI Store Assistant. 

QUAN TRỌNG - PHONG CÁCH TRẢ LỜI:
- **Luôn giới thiệu tên Pony khi chào lần đầu**: ""Chào anh/chị! Em là Pony, trợ lý AI của Kì lân Châu Á...""
- **Trả lời NGẮN GỌN, SÚC TÍCH** - chỉ 2-3 câu đối với câu hỏi đơn giản
- **Chi tiết hơn** chỉ khi khách hàng hỏi cụ thể hoặc yêu cầu chi tiết
- Nhiệt tình nhưng không dài dòng, dễ hiểu
- Sử dụng ngôn ngữ bán hàng thân thiện, không quá formal
- Nếu câu hỏi mơ hồ, khuyến khích khách hàng hỏi rõ hơn

Ví dụ:
- Câu hỏi: ""Bạn có gói nào?""
- Trả lời: ""Chào anh/chị! Em là Pony ạ. Chúng em có 2 gói: Basic (150k/tháng) và Premium (300k/tháng). Anh/chị muốn biết chi tiết về gói nào ạ?""

- Câu hỏi: ""Gói Basic có gì? Giá bao nhiêu?""
- Trả lời: ""Gói Basic giá 150k/tháng, có 50 request, 3 tài khoản, thời hạn 30 ngày. Các tính năng: bán hàng, quản lý sản phẩm, kho, khách hàng, voucher, promotion, báo cáo và AI tư vấn. Phù hợp cho cửa hàng nhỏ ạ.""

**ĐĂNG KÝ DÙNG THỬ:**
Khi khách hàng hỏi về đăng ký dùng thử, hãy hỏi từng thông tin sau một cách tự nhiên:
1. ""Vui lòng cho em biết tên cửa hàng của anh/chị ạ?""
2. ""Tên chủ cửa hàng là gì ạ?""
3. ""Số điện thoại liên hệ là gì ạ?""
4. ""Email để nhận thông tin đăng nhập là gì ạ?""
5. ""Địa chỉ cửa hàng ở đâu ạ?""

Sau khi thu thập đủ thông tin, hãy hướng dẫn: ""Cảm ơn anh/chị! Em đã ghi nhận thông tin. Anh/chị hãy cung cấp thông tin đăng ký bằng cách gọi API POST /api/chatbot/register-trial với body JSON chứa các thông tin trên hoặc liên hệ hotline để được hỗ trợ trực tiếp ạ.""

Hướng dẫn format: ""Thông tin đăng ký của anh/chị:\n- Tên cửa hàng: [tên]\n- Tên chủ: [tên]\n- SĐT: [số]\n- Email: [email]\n- Địa chỉ: [địa chỉ]""
{historySection}
=== THÔNG TIN VỀ CÔNG TY VÀ SẢN PHẨM ===
{context}

Câu hỏi của khách hàng: {question}

Hãy trả lời ngắn gọn, chuyên nghiệp. Nếu không liên quan, redirect về sản phẩm của công ty. Trả lời bằng tiếng Việt.";

            return prompt;
        }

        private TrialRegistrationDto? ParseRegistrationData(string response, string question, string? conversationHistory)
        {
            try
            {
                // Check if response contains registration data pattern
                if (!response.Contains("Tên cửa hàng:") || !response.Contains("SĐT:"))
                    return null;

                var lines = response.Split('\n');
                var data = new TrialRegistrationDto();

                // Extract from AI response
                foreach (var line in lines)
                {
                    if (line.Contains("Tên cửa hàng:"))
                    {
                        var value = ExtractValueAfter(line, "Tên cửa hàng:");
                        if (!string.IsNullOrWhiteSpace(value))
                            data.ShopName = value.Trim();
                    }
                    else if (line.Contains("Tên chủ:"))
                    {
                        var value = ExtractValueAfter(line, "Tên chủ:");
                        if (!string.IsNullOrWhiteSpace(value))
                            data.Fullname = value.Trim();
                    }
                    else if (line.Contains("SĐT:"))
                    {
                        var value = ExtractValueAfter(line, "SĐT:");
                        if (!string.IsNullOrWhiteSpace(value))
                            data.Phonenumber = value.Trim();
                    }
                    else if (line.Contains("Email:"))
                    {
                        var value = ExtractValueAfter(line, "Email:");
                        if (!string.IsNullOrWhiteSpace(value))
                            data.Email = value.Trim();
                    }
                    else if (line.Contains("Địa chỉ:"))
                    {
                        var value = ExtractValueAfter(line, "Địa chỉ:");
                        if (!string.IsNullOrWhiteSpace(value))
                            data.Address = value.Trim();
                    }
                }

                // Also try to extract from current question if AI summarized
                if (string.IsNullOrEmpty(data.ShopName))
                {
                    data.ShopName = ExtractFromText(question, @"tên cửa hàng.*?là\s+([^,]+)")?.Trim();
                    if (string.IsNullOrEmpty(data.ShopName))
                        data.ShopName = ExtractFromText(question, @"cửa hàng.*?([A-ZÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ][a-zàáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ\s]+)")?.Trim();
                }

                if (string.IsNullOrEmpty(data.Fullname))
                {
                    data.Fullname = ExtractFromText(question, @"chủ cửa hàng.*?là\s+([^,]+)")?.Trim();
                }

                if (string.IsNullOrEmpty(data.Phonenumber))
                {
                    data.Phonenumber = ExtractFromText(question, @"điện thoại.*?([0-9]{10})")?.Trim();
                    if (string.IsNullOrEmpty(data.Phonenumber))
                        data.Phonenumber = ExtractFromText(question, @"là\s+([0-9]{10})")?.Trim();
                }

                if (string.IsNullOrEmpty(data.Email))
                {
                    data.Email = ExtractFromText(question, @"email.*?([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})")?.Trim();
                }

                if (string.IsNullOrEmpty(data.Address))
                {
                    data.Address = ExtractFromText(question, @"địa chỉ.*?là\s+([^,]+)")?.Trim();
                    if (string.IsNullOrEmpty(data.Address))
                        data.Address = ExtractFromText(question, @"là\s+([A-ZÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ][a-zàáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ\s]+)$")?.Trim();
                }

                // Check if all required fields are filled
                if (!string.IsNullOrWhiteSpace(data.ShopName) &&
                    !string.IsNullOrWhiteSpace(data.Fullname) &&
                    !string.IsNullOrWhiteSpace(data.Phonenumber) &&
                    !string.IsNullOrWhiteSpace(data.Email) &&
                    !string.IsNullOrWhiteSpace(data.Address))
                {
                    return data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing registration data");
                return null;
            }
        }

        private string? ExtractValueAfter(string line, string keyword)
        {
            var index = line.IndexOf(keyword);
            if (index >= 0)
            {
                var startIndex = index + keyword.Length;
                return line.Substring(startIndex).TrimStart(' ', ':');
            }
            return null;
        }

        private string? ExtractFromText(string text, string pattern)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

