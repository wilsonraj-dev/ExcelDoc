using System.Net;
using System.Net.Mail;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpOptions _smtpOptions;

        public EmailService(IOptions<SmtpOptions> smtpOptions, ILogger<EmailService> logger)
        {
            _logger = logger;
            _smtpOptions = smtpOptions.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_smtpOptions.Host) || string.IsNullOrWhiteSpace(_smtpOptions.FromEmail))
            {
                _logger.LogWarning("SMTP não configurado. O e-mail para {Email} foi registrado apenas em log.", toEmail);
                _logger.LogInformation("Assunto: {Subject}. Conteúdo: {Body}", subject, body);
                return;
            }

            using var message = new MailMessage
            {
                From = string.IsNullOrWhiteSpace(_smtpOptions.FromName)
                    ? new MailAddress(_smtpOptions.FromEmail)
                    : new MailAddress(_smtpOptions.FromEmail, _smtpOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
            {
                client.Credentials = new NetworkCredential(_smtpOptions.UserName, _smtpOptions.Password);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message);
        }
    }
}
