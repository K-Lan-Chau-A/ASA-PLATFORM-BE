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
        private readonly UserRepo _userRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public SepayWebhookController(
            ILogger<SepayWebhookController> logger,
            IOrderService orderService,
            ShopRepo shopRepo,
            IConfiguration config,
            UserRepo userRepo,
            IEmailService emailService)
        {
            _logger = logger;
            _orderService = orderService;
            _shopRepo = shopRepo;
            _config = config;
            _userRepo = userRepo;
            _emailService = emailService;
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
                        var content = payload.content.Trim();
                        var sevqrIndex = content.IndexOf("SEVQR", StringComparison.OrdinalIgnoreCase);
                        if (sevqrIndex > 0)
                        {
                            // Ưu tiên lấy chuỗi số LIỀN TRƯỚC từ "SEVQR" (không phụ thuộc dấu '-')
                            int i = sevqrIndex - 1;
                            var digits = new System.Text.StringBuilder();
                            while (i >= 0 && char.IsDigit(content[i]))
                            {
                                digits.Insert(0, content[i]);
                                i--;
                            }

                            bool lookedUp = false;
                            if (digits.Length > 0 && long.TryParse(digits.ToString(), out long tightOrderId))
                            {
                                var orderResult = await _orderService.GetByIdAsync(tightOrderId);
                                if (orderResult.Success)
                                {
                                    order = orderResult.Data;
                                    lookedUp = true;
                                }
                            }

                            // Nếu không tìm được theo chuỗi số liền trước, fallback lấy token sau dấu '-' cuối trước SEVQR
                            if (!lookedUp)
                            {
                                var beforeSevqr = content.Substring(0, sevqrIndex).TrimEnd('-');
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

                // Tính và cập nhật ExpiredAt từ Product.Duration sau khi thanh toán thành công
                try
                {
                    var updateExpiry = await _orderService.UpdateExpiryFromProductAsync(order.OrderId);
                    if (!updateExpiry.Success)
                    {
                        _logger.LogWarning("Không thể cập nhật ExpiredAt cho Order {OrderId}: {Message}", order.OrderId, updateExpiry.Message);
                    }
                }
                catch (Exception exUpd)
                {
                    _logger.LogWarning(exUpd, "Lỗi khi cập nhật ExpiredAt cho Order {OrderId}", order.OrderId);
                }

                // Sau khi thanh toán thành công: gửi email thông tin đăng nhập cho email trong shop
                try
                {
                    if (order.ShopId.HasValue)
                    {
                        var shop = await _shopRepo.GetByIdAsync(order.ShopId.Value);
                        if (shop != null && !string.IsNullOrWhiteSpace(shop.Email))
                        {
                            // Tạo thông tin đăng nhập từ shop phone (thường dùng làm username)
                            var username = shop.Phonenumber;
                            var password = "asa123456"; // Tạo password mặc định
                            
                            var displayName = shop.Fullname ?? shop.ShopName ?? username;
                            var subject = "Xác nhận thanh toán - ASA Platform";
                            var body = $@"
                            <!DOCTYPE html>
                            <html lang='vi'>
                            <head>
                                <meta charset='UTF-8'>
                                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                <title>Thanh toán thành công</title>
                                <style>
                                    @media only screen and (max-width: 600px) {{
                                        .container {{ margin: 12px !important; padding: 16px !important; }}
                                        .header {{ padding: 20px 16px !important; }}
                                        .content {{ padding: 20px 16px !important; }}
                                        .info-card {{ padding: 12px 14px !important; margin: 12px 0 !important; }}
                                        .credential-grid {{ flex-direction: column !important; gap: 8px !important; }}
                                        .credential-item {{ flex: none !important; }}
                                        .cta-button {{ width: 100% !important; text-align: center !important; padding: 14px 18px !important; }}
                                        .footer {{ padding: 12px 16px !important; }}
                                        h2 {{ font-size: 20px !important; }}
                                        .subtitle {{ font-size: 14px !important; }}
                                    }}
                                </style>
                            </head>
                            <body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif; background: #f8fafc; padding: 20px; margin: 0; line-height: 1.6;'>
                                <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1); overflow: hidden;'>
                                    <!-- Header -->
                                    <div class='header' style='background: linear-gradient(135deg, rgb(1, 109, 115), rgb(0, 85, 90)); padding: 32px 28px; color: #ffffff; text-align: center; position: relative;'>
                                        <div style='position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: url(data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAxMDAgMTAwIj48ZGVmcz48cGF0dGVybiBpZD0iZ3JhaW4iIHdpZHRoPSIxMDAiIGhlaWdodD0iMTAwIiBwYXR0ZXJuVW5pdHM9InVzZXJTcGFjZU9uVXNlIj48Y2lyY2xlIGN4PSI1MCIgY3k9IjUwIiByPSIxIiBmaWxsPSJ3aGl0ZSIgb3BhY2l0eT0iMC4xIi8+PC9wYXR0ZXJuPjwvZGVmcz48cmVjdCB3aWR0aD0iMTAwIiBoZWlnaHQ9IjEwMCIgZmlsbD0idXJsKCNncmFpbikiLz48L3N2Zz4=) repeat; opacity: 0.3;'></div>
                                        <div style='position: relative; z-index: 1;'>
                                            <div style='width: 60px; height: 60px; background: rgba(255, 255, 255, 0.2); border-radius: 50%; margin: 0 auto 16px; display: flex; align-items: center; justify-content: center; backdrop-filter: blur(10px);'>
                                                <svg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
                                                    <path d='M20 6L9 17l-5-5'/>
                                                </svg>
                                            </div>
                                            <h2 style='margin: 0 0 8px; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;'>Thanh toán thành công</h2>
                                            <p class='subtitle' style='margin: 0; opacity: 0.9; font-size: 16px; font-weight: 400;'>Xin chào {displayName}</p>
                                        </div>
                                    </div>
                                    
                                    <!-- Content -->
                                    <div class='content' style='padding: 32px 28px; color: #1a202c;'>
                                        <div style='text-align: center; margin-bottom: 24px;'>
                                            <p style='margin: 0 0 16px; font-size: 18px; color: #2d3748; font-weight: 500;'>Đơn hàng <span style='color: rgb(1, 109, 115); font-weight: 700;'>#{order.OrderId}</span> đã được thanh toán thành công!</p>
                                            <p style='margin: 0; color: #4a5568; font-size: 16px;'>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ của chúng tôi.</p>
                                        </div>
                                        
                                        <!-- Credentials Card -->
                                        <div class='info-card' style='background: linear-gradient(135deg, #f7fafc, #edf2f7); border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px 18px; margin: 24px 0; position: relative;'>
                                            <div style='display: flex; align-items: center; margin-bottom: 16px;'>
                                                <div style='width: 40px; height: 40px; background: rgb(1, 109, 115); border-radius: 8px; display: flex; align-items: center; justify-content: center; margin-right: 12px;'>
                                                    <svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
                                                        <rect x='3' y='11' width='18' height='11' rx='2' ry='2'/>
                                                        <path d='M7 11V7a5 5 0 0 1 10 0v4'/>
                                                    </svg>
                                                </div>
                                                <h3 style='margin: 0; font-size: 18px; color: #1a202c; font-weight: 600;'>Thông tin truy cập hệ thống</h3>
                                            </div>
                                            
                                            <div class='credential-grid' style='display: flex; gap: 16px; flex-wrap: wrap;'>
                                                <div class='credential-item' style='flex: 1 1 250px; background: #ffffff; border: 2px solid #e2e8f0; border-radius: 10px; padding: 16px 14px; transition: all 0.3s ease;'>
                                                    <div style='font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 0.8px; font-weight: 600; margin-bottom: 6px;'>Tên đăng nhập</div>
                                                    <div style='font-weight: 700; color: #1a202c; font-size: 16px; font-family: Monaco, Consolas, monospace; background: #f7fafc; padding: 8px 10px; border-radius: 6px; border: 1px solid #e2e8f0;'>{username}</div>
                                                </div>
                                                <div class='credential-item' style='flex: 1 1 250px; background: #ffffff; border: 2px solid #e2e8f0; border-radius: 10px; padding: 16px 14px; transition: all 0.3s ease;'>
                                                    <div style='font-size: 11px; color: #718096; text-transform: uppercase; letter-spacing: 0.8px; font-weight: 600; margin-bottom: 6px;'>Mật khẩu</div>
                                                    <div style='font-weight: 700; color: #1a202c; font-size: 16px; font-family: Monaco, Consolas, monospace; background: #f7fafc; padding: 8px 10px; border-radius: 6px; border: 1px solid #e2e8f0;'>{password}</div>
                                                </div>
                                            </div>
                                            
                                            <div style='background: #fff5f5; border: 1px solid #fed7d7; border-radius: 8px; padding: 12px 14px; margin-top: 16px;'>
                                                <div style='display: flex; align-items: center;'>
                                                    <svg width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='#c53030' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='margin-right: 8px;'>
                                                        <path d='M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z'/>
                                                        <line x1='12' y1='9' x2='12' y2='13'/>
                                                        <line x1='12' y1='17' x2='12.01' y2='17'/>
                                                    </svg>
                                                    <p style='margin: 0; color: #c53030; font-size: 13px; font-weight: 500;'>Vui lòng bảo mật thông tin này và đổi mật khẩu sau khi đăng nhập lần đầu.</p>
                                                </div>
                                            </div>
                                        </div>
                                        
                                        <!-- CTA Button -->
                                        <div style='text-align: center; margin: 32px 0;'>
                                            <a href='https://asa-web-app-tawny.vercel.app/login' class='cta-button' style='display: inline-block; background: linear-gradient(135deg, rgb(1, 109, 115), rgb(0, 85, 90)); color: #ffffff; text-decoration: none; padding: 16px 32px; border-radius: 12px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 14px rgba(1, 109, 115, 0.3); transition: all 0.3s ease; position: relative; overflow: hidden;'>
                                                <span style='position: relative; z-index: 1;'>Truy cập hệ thống ngay</span>
                                                <div style='position: absolute; top: 0; left: -100%; width: 100%; height: 100%; background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent); transition: left 0.5s;'></div>
                                            </a>
                                        </div>
                                        
                                        <!-- Additional Info -->
                                        <div style='background: #f7fafc; border-radius: 10px; padding: 20px; border-left: 4px solid rgb(1, 109, 115);'>
                                            <h4 style='margin: 0 0 12px; color: #2d3748; font-size: 16px; font-weight: 600;'>Hỗ trợ khách hàng</h4>
                                            <p style='margin: 0 0 8px; color: #4a5568; font-size: 14px;'>Nếu bạn gặp bất kỳ khó khăn nào trong quá trình sử dụng, vui lòng liên hệ:</p>
                                            <ul style='margin: 0; padding-left: 20px; color: #4a5568; font-size: 14px;'>
                                                <li>Email: support@asa-platform.com</li>
                                                <li>Hotline: 1900-xxxx</li>
                                                <li>Giờ làm việc: 8:00 - 17:00 (Thứ 2 - Thứ 6)</li>
                                            </ul>
                                        </div>
                                    </div>
                                    
                                    <!-- Footer -->
                                    <div class='footer' style='background: #1a202c; color: #a0aec0; padding: 20px 28px; text-align: center; border-top: 1px solid #2d3748;'>
                                        <div style='margin-bottom: 12px;'>
                                            <img src='data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxMjAiIGhlaWdodD0iMjQiIHZpZXdCb3g9IjAgMCAxMjAgMjQiPjx0ZXh0IHg9IjYwIiB5PSIxNiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZmlsbD0icmdiKDEsMTA5LDExNSkiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxNCIgZm9udC13ZWlnaHQ9ImJvbGQiPkFTQSBTbGF0Zm9ybTwvdGV4dD48L3N2Zz4=' alt='ASA Platform' style='height: 24px;'>
                                        </div>
                                        <p style='margin: 0 0 8px; font-size: 13px; color: #a0aec0;'>&copy; {DateTime.Now.Year} ASA Platform. Tất cả các quyền được bảo lưu.</p>
                                        <p style='margin: 0; font-size: 12px; color: #718096;'>Email này được gửi tự động, vui lòng không trả lời.</p>
                                    </div>
                                </div>
                            </body>
                            </html>";
                            
                            var emailSent = await _emailService.SendEmailAsync(shop.Email, subject, body);
                            if (emailSent)
                            {
                                _logger.LogInformation("Đã gửi email thông tin tài khoản tới {Email} cho Order {OrderId}", shop.Email, order.OrderId);
                            }
                            else
                            {
                                _logger.LogError("Không thể gửi email thông tin tài khoản tới {Email} cho Order {OrderId}", shop.Email, order.OrderId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Shop {ShopId} không có email hoặc không tồn tại; bỏ qua gửi email.", order.ShopId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Order {OrderId} không có ShopId; bỏ qua gửi email.", order.OrderId);
                    }
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "Lỗi khi gửi email thông tin tài khoản cho Order {OrderId}", order.OrderId);
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
