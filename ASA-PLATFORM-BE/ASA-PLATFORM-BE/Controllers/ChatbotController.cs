using ASA_PLATFORM_SERVICE.DTOs;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ASA_PLATFORM_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly IShopService _shopService;

        public ChatbotController(IChatbotService chatbotService, IShopService shopService)
        {
            _chatbotService = chatbotService;
            _shopService = shopService;
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatbotResponseDto>> Ask([FromBody] ChatbotRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new ChatbotResponseDto
                    {
                        Answer = "Vui lòng nhập câu hỏi.",
                        IsSuccess = false,
                        ErrorMessage = "Question is required"
                    });
                }

                var response = await _chatbotService.ProcessQuestionAsync(request);

                if (!response.IsSuccess)
                {
                    return StatusCode(500, response);
                }

                // Check if AI has collected enough info to auto-register
                if (response.IsReadyToRegister && response.RegistrationData != null)
                {
                    try
                    {
                        var shopRequest = new ShopRequest
                        {
                            shopName = response.RegistrationData.ShopName,
                            Fullname = response.RegistrationData.Fullname,
                            Phonenumber = response.RegistrationData.Phonenumber,
                            Email = response.RegistrationData.Email,
                            address = response.RegistrationData.Address,
                            status = 2,
                            ProductId = 1,
                            ShopToken = "",
                            QrcodeUrl = "",
                            SepayApiKey = "",
                            BankName = "",
                            BankCode = "",
                            BankNum = ""
                        };

                        var createResult = await _shopService.CreateAsync(shopRequest);
                        
                        if (createResult.Success)
                        {
                            response.Answer = $"Cảm ơn anh/chị đã đăng ký dùng thử AI Store Assistant! Thông tin đăng nhập sẽ được gửi qua email {response.RegistrationData.Email} trong vài phút tới. Anh/chị có thể đăng nhập và bắt đầu sử dụng ngay ạ!";
                        }
                        else
                        {
                            response.Answer = $"Xin lỗi, có lỗi xảy ra khi đăng ký: {createResult.Message}. Vui lòng thử lại sau hoặc liên hệ hotline để được hỗ trợ ạ.";
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Answer += $"\n\n⚠️ Có lỗi xảy ra: {ex.Message}";
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ChatbotResponseDto
                {
                    Answer = "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new
            {
                Status = "OK",
                Message = "Chatbot service is running",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

