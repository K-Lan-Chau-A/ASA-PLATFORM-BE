using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ASA_PLATFORM_SERVICE.Interface;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_REPO.Repository;
using Microsoft.Extensions.Configuration;
// using ASA_PLATFORM_SERVICE.DTOs.Request;

namespace ASA_PLATFORM_BE.Controllers
{
    [ApiController]
    [Route("api/sepay")]
    public class SepayWebhookController : ControllerBase
    {
        private readonly ILogger<SepayWebhookController> _logger;
        private readonly IOrderService _orderService;
        private readonly ShopRepo _shopRepo;
        private readonly IConfiguration _config;

        public SepayWebhookController(
            ILogger<SepayWebhookController> logger,
            IOrderService orderService,
            ShopRepo shopRepo,
            IConfiguration config)
        {
            _logger = logger;
            _orderService = orderService;
            _shopRepo = shopRepo;
            _config = config;
        }

        [HttpGet("vietqr")]
        public async Task<IActionResult> GenerateVietQr([FromQuery] long orderId)
        {
            try
            {
                // Lấy order
                var orderResult = await _orderService.GetByIdAsync(orderId);
                if (!orderResult.Success || orderResult.Data == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }
                var order = orderResult.Data;

                // Lấy Shop theo ShopId từ Order
                if (order.ShopId == null)
                {
                    return BadRequest(new { success = false, message = "Order missing ShopId" });
                }

                // var shops = await _shopRepo.GetAllAsync();
                // var shop = shops.FirstOrDefault(s => s.ShopId == order.ShopId);
                // if (shop == null)
                // {
                //     return NotFound(new { success = false, message = "Shop not found" });
                // }
                // if (shop.Status != 1)
                // {
                //     return BadRequest(new { success = false, message = "Shop is not active" });
                // }

                // Tính số tiền cần thanh toán
                var total = order.TotalPrice ?? 0m;
                if (total <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid order amount" });
                }

                // Sử dụng thông tin ngân hàng hardcode
                var bankCode = "970422"; // MBBank
                var bankAccount = "55501012004"; // Số tài khoản mặc định
                var baseUrl = $"https://img.vietqr.io/image/{bankCode}-{bankAccount}-compact2.png";

                // Tạo query theo tài liệu VietQR: amount, addInfo, accountName
                var delimiter = baseUrl.Contains('?') ? "&" : "?";
                var amount = (long)decimal.Round(total, 0, MidpointRounding.AwayFromZero);
                var addInfo = Uri.EscapeDataString($"{order.OrderId}-SEVQR");
                var accName = Uri.EscapeDataString("Kỳ Lân Châu Á");
                var qrUrl = $"{baseUrl}{delimiter}amount={amount}&addInfo={addInfo}&accountName={accName}";

                return Ok(new { success = true, url = qrUrl, orderId = order.OrderId, amount = amount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo VietQR cho order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // Model map từ payload SePay
        public class SepayWebhookPayload
        {
            public long id { get; set; }
            public string gateway { get; set; }
            public string transactionDate { get; set; }
            public string accountNumber { get; set; }
            public string? code { get; set; }
            public string content { get; set; }
            public string transferType { get; set; }
            public long transferAmount { get; set; }
            public long? accumulated { get; set; }
            public string? subAccount { get; set; }
            public string referenceCode { get; set; }
            public string description { get; set; }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] SepayWebhookPayload payload)
        {
            try
            {
                // Kiểm tra Apikey (nếu có cấu hình)
                var configuredApiKey = _config["Sepay:WebhookApiKey"];
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(configuredApiKey))
                {
                    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Apikey "))
                    {
                        return Unauthorized(new { success = false, message = "Missing Apikey" });
                    }
                    var apiKey = authHeader.Replace("Apikey ", "").Trim();
                    if (!string.Equals(apiKey, configuredApiKey, StringComparison.Ordinal))
                    {
                        return Unauthorized(new { success = false, message = "Invalid Apikey" });
                    }
                }

                if (payload == null || payload.id <= 0 || payload.transferAmount <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid payload" });
                }

                // Tìm Order theo content (ưu tiên): lấy số trước "SEVQR" làm orderId
                OrderResponse order = null;
                if (!string.IsNullOrWhiteSpace(payload.content))
                {
                    try
                    {
                        var trimmedContent = payload.content.Trim();
                        var sevqrIndex = trimmedContent.IndexOf("SEVQR", StringComparison.OrdinalIgnoreCase);
                        if (sevqrIndex > 0)
                        {
                            var beforeSevqr = trimmedContent.Substring(0, sevqrIndex).TrimEnd('-');
                            var lastDashIndex = beforeSevqr.LastIndexOf('-');
                            var orderIdSegment = lastDashIndex >= 0 ? beforeSevqr.Substring(lastDashIndex + 1) : beforeSevqr;
                            if (long.TryParse(orderIdSegment, out long contentOrderId))
                            {
                                var orderResult = await _orderService.GetByIdAsync(contentOrderId);
                                if (orderResult.Success)
                                {
                                    order = orderResult.Data;
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Nếu chưa tìm thấy, thử theo referenceCode
                if (order == null && !string.IsNullOrEmpty(payload.referenceCode))
                {
                    if (long.TryParse(payload.referenceCode, out long refOrderId))
                    {
                        var orderResult = await _orderService.GetByIdAsync(refOrderId);
                        if (orderResult.Success) order = orderResult.Data;
                    }
                    if (order == null)
                    {
                        var orderResult = await _orderService.GetByNoteAsync(payload.referenceCode);
                        if (orderResult.Success) order = orderResult.Data;
                    }
                }

                if (order == null)
                {
                    _logger.LogWarning("Không tìm thấy Order với referenceCode: {ReferenceCode}", payload.referenceCode);
                    return BadRequest(new { success = false, message = "Order not found" });
                }

                // Đảm bảo Shop tồn tại trên Platform theo order.ShopId
                if (order.ShopId == null)
                {
                    return BadRequest(new { success = false, message = "Order missing ShopId" });
                }

                var existingShop = await _shopRepo.GetByIdAsync(order.ShopId.Value);
                if (existingShop == null)
                {
                    return BadRequest(new { success = false, message = "Shop not found" });
                }

                // Nếu đã thanh toán rồi thì bỏ qua
                if (order.Status == 1)
                {
                    return Ok(new { success = true, info = "order_already_paid" });
                }

                // Cập nhật trạng thái Order thành đã thanh toán (1)
                var updateStatusResult = await _orderService.UpdateStatusAsync(order.OrderId, 1);
                if (!updateStatusResult.Success)
                {
                    _logger.LogWarning("Không thể cập nhật status Order {OrderId}: {Message}", order.OrderId, updateStatusResult.Message);
                }

                // Cập nhật trạng thái Shop thành active (1)
                try
                {
                    existingShop.Status = 1;
                    await _shopRepo.UpdateAsync(existingShop);
                }
                catch (Exception sx)
                {
                    _logger.LogWarning(sx, "Không thể cập nhật status Shop {ShopId} thành 1", existingShop.ShopId);
                }

                _logger.LogInformation("Xử lý webhook SePay thành công cho Order {OrderId}. Amount: {Amount}", order.OrderId, payload.transferAmount);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý webhook SePay");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
