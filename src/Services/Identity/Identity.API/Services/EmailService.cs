using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Identity.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"]?.Replace(" ", "");

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword)
                    || senderPassword == "thay_bang_app_password_cua_ban_tai_day")
                {
                    var msg = "Email settings are not configured properly. Falling back to mock email.";
                    _logger.LogWarning(msg);
                    Console.WriteLine($"[EmailService] {msg}");
                    _logger.LogInformation("=================================================");
                    _logger.LogInformation("MOCK EMAIL SENT TO: {To}", to);
                    _logger.LogInformation("SUBJECT: {Subject}", subject);
                    _logger.LogInformation("BODY: {Body}", body);
                    _logger.LogInformation("=================================================");
                    return;
                }

                Console.WriteLine($"[EmailService] Attempting to send REAL email to {to} via {smtpServer}:{smtpPort}...");

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {To}", to);
                Console.WriteLine($"[EmailService] SUCCESS: Email sent to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                Console.WriteLine($"[EmailService] ERROR: Failed to send email to {to}. Exception: {ex.Message}");
                throw new Exception("Lỗi khi gửi email: " + ex.Message);
            }
        }

        public Task SendOtpEmailAsync(string to, string otpCode)
        {
            var subject = "Mã xác thực OTP - ClothesShop Tutorial";
            var body = $@"
                <h3>Xác thực quên mật khẩu</h3>
                <p>Chào bạn,</p>
                <p>Mã OTP của bạn là: <strong style='font-size: 20px; color: #007bff;'>{otpCode}</strong></p>
                <p>Mã này sẽ hết hạn sau 10 phút.</p>
                <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                <br/>
                <p>Trân trọng,</p>
                <p>ClothesShop Team</p>";

            return SendEmailAsync(to, subject, body);
        }
    }
}
