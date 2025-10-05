using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly PasswordResetRepo _passwordResetRepo;
        private readonly UserRepo _userRepo;
        private readonly ILogger<EmailService> _logger;




        public EmailService(IConfiguration config,PasswordResetRepo passwordResetRepo, UserRepo userRepo, ILogger<EmailService> logger)
        {
            _config = config;
            _passwordResetRepo = passwordResetRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                // Validate SMTP settings - try environment variables first, then config
                var host = Environment.GetEnvironmentVariable("SMTP_SETTINGS__HOST") ?? _config["SmtpSettings:Host"];
                var portStr = Environment.GetEnvironmentVariable("SMTP_SETTINGS__PORT") ?? _config["SmtpSettings:Port"];
                var username = Environment.GetEnvironmentVariable("SMTP_SETTINGS__USERNAME") ?? _config["SmtpSettings:Username"];
                var password = Environment.GetEnvironmentVariable("SMTP_SETTINGS__PASSWORD") ?? _config["SmtpSettings:Password"];

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portStr) || 
                    string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogError("SMTP settings are missing or invalid");
                    return false;
                }

                if (!int.TryParse(portStr, out int port))
                {
                    _logger.LogError($"Invalid SMTP port: {portStr}");
                    return false;
                }

                _logger.LogInformation($"Attempting to send email to {to} via {host}:{port}");
                
                // Log configuration source for debugging
                _logger.LogInformation($"SMTP Config - Host: {host}, Port: {port}, Username: {username}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("ASA Platform", username));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using var client = new SmtpClient();
                
                // Try different SSL options
                try
                {
                    await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                }
                catch
                {
                    _logger.LogWarning("StartTls failed, trying Auto");
                    await client.ConnectAsync(host, port, SecureSocketOptions.Auto);
                }

                await client.AuthenticateAsync(username, password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}. Error: {ex.Message}. StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName, string username, string password)
        {
            var subject = "Chào mừng đến với AI Store Assistant";
            var body = $@"
        <html>
        <body style='font-family:Arial,Helvetica,sans-serif;background:#f6f9fc;padding:24px;'>
            <div style='max-width:640px;margin:0 auto;background:#ffffff;border-radius:12px;box-shadow:0 8px 24px rgba(0,0,0,0.08);overflow:hidden;'>
                <div style='background:linear-gradient(135deg,#4f46e5,#06b6d4);padding:24px 28px;color:#ffffff;'>
                    <h2 style='margin:0;font-size:22px;'>Chào mừng đến với AI Store Assistant 🎉</h2>
                    <p style='margin:6px 0 0;opacity:0.95;'>Xin chào {userName}, cảm ơn bạn đã đăng ký dùng thử!</p>
                </div>

                <div style='padding:24px 28px;color:#0f172a;'>
                    <p style='margin:0 0 12px;'>Cảm ơn bạn đã đăng ký dùng thử sản phẩm của chúng tôi. Thời gian dùng thử của bạn là <strong>7 ngày</strong>.</p>
                    <p style='margin:0 0 16px;'>Chúc bạn sẽ có những trải nghiệm thật tốt với sản phẩm của chúng tôi!</p>

                    <div style='background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:16px 18px;margin:18px 0;'>
                        <h3 style='margin:0 0 10px;font-size:16px;color:#334155;'>Thông tin đăng nhập</h3>
                        <div style='display:flex;gap:12px;flex-wrap:wrap;'>
                            <div style='flex:1 1 240px;background:#ffffff;border:1px solid #e2e8f0;border-radius:8px;padding:10px 12px;'>
                                <div style='font-size:12px;color:#64748b;text-transform:uppercase;letter-spacing:0.4px;'>Username</div>
                                <div style='font-weight:600;color:#0f172a;margin-top:4px;'>{username}</div>
                            </div>
                            <div style='flex:1 1 240px;background:#ffffff;border:1px solid #e2e8f0;border-radius:8px;padding:10px 12px;'>
                                <div style='font-size:12px;color:#64748b;text-transform:uppercase;letter-spacing:0.4px;'>Password</div>
                                <div style='font-weight:600;color:#0f172a;margin-top:4px;'>{password}</div>
                            </div>
                        </div>
                        <p style='margin:10px 0 0;color:#64748b;font-size:12px;'>Vui lòng bảo mật thông tin đăng nhập này. Bạn có thể đổi mật khẩu sau khi đăng nhập.</p>
                    </div>

                    <a href='https://asa-web-app-tawny.vercel.app/login'
                       style='display:inline-block;background:#4f46e5;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:10px;font-weight:600;'>Đăng nhập ngay</a>

                    <p style='margin:18px 0 0;color:#475569;font-size:14px;'>Nếu bạn cần hỗ trợ, hãy phản hồi lại email này hoặc liên hệ đội ngũ hỗ trợ của chúng tôi.</p>
                </div>

                <div style='background:#0f172a;color:#94a3b8;padding:16px 28px;font-size:12px;'>
                    © {DateTime.Now.Year} AI Store Assistant. Tất cả các quyền được bảo lưu.
                </div>
            </div>
        </body>
        </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }

        private string GenerateOtp(int length = 6)
        {
            var random = new Random();
            return string.Concat(Enumerable.Range(0, length).Select(_ => random.Next(0, 10)));
        }
        private static DateTime GetVietnamNow()
        {
            var utcNow = DateTime.UtcNow;
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            return DateTime.SpecifyKind(vietnamTime, DateTimeKind.Unspecified); // để khớp với TIMESTAMP WITHOUT TIME ZONE
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail)
        {
            var emailExists = await _userRepo.IsEmailExistsAsync(toEmail);
            if (!emailExists)
                return false;

            var otp = GenerateOtp();
            var now = GetVietnamNow();
            var expirationTime = now.AddMinutes(10);
            var otpEntry = new PasswordResetOtp
            {
                Email = toEmail,
                Otp = otp,
                CreatedAt = now,
                ExpiredAt = expirationTime,
                IsUsed = false
            };
            await _passwordResetRepo.CreateAsync(otpEntry);

            var subject = "Password Reset Request - EDUConnect";
            var body = $@"
                <html>
                <body>
                    <h2>Reset Your Password</h2>
                    <p>We received a request to reset your password for your EDUConnect account.</p>
                    <p>Your OTP code is:</p>
                    <h3 style='color:#007bff;'>{otp}</h3>
                    <p>This code will expire in 10 minutes. Please do not share it with anyone.</p>
                    <p>If you did not request a password reset, please ignore this email.</p>
                    <br>
                    <p>Best regards,<br>The EDUConnect Team</p>
                </body>
                </html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
    }
}
