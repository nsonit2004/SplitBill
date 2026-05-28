using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SB_Services.Interfaces;

namespace SB_Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var server = _configuration["Smtp:Server"];
            var portStr = _configuration["Smtp:Port"];
            var senderEmail = _configuration["Smtp:SenderEmail"];
            var senderName = _configuration["Smtp:SenderName"] ?? "VietQR SplitBill Pro";
            var password = _configuration["Smtp:Password"];

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
            {
                // Fallback: ghi log khi chạy môi trường dev chưa cấu hình Email SMTP
                _logger.LogInformation($"[MOCK EMAIL SENT] To: {toEmail} | Subject: {subject} | Body: {body}");
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                int port = int.TryParse(portStr, out int p) ? p : 587;
                
                await client.ConnectAsync(server, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                
                _logger.LogInformation($"Email đã gửi thành công tới {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Gửi email tới {toEmail} thất bại.");
            }
        }
    }
}
