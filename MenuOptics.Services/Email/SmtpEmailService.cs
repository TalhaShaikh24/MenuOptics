using System.IO;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace MenuOptics.Services
{
    public class SmtpEmailService : ISmtpEmailService
    {
        private readonly IConfiguration _config;
        public SmtpEmailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? attachmentFilePath = null, string? attachmentDisplayName = null)
        {
            var smtp = _config["Email:SmtpHost"]!;
            var port = int.Parse(_config["Email:SmtpPort"]!);
            var user = _config["Email:Username"]!;
            var pass = _config["Email:Password"]!;
            var from = _config["Email:From"]!;

            using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(user, pass), EnableSsl = true };
            using var message = new MailMessage(from, toEmail, subject, htmlBody) { IsBodyHtml = true };

            if (!string.IsNullOrWhiteSpace(attachmentFilePath) && File.Exists(attachmentFilePath))
            {
                var attachment = new Attachment(attachmentFilePath);
                if (!string.IsNullOrWhiteSpace(attachmentDisplayName))
                {
                    attachment.Name = attachmentDisplayName;
                }
                message.Attachments.Add(attachment);
            }

            await client.SendMailAsync(message);
        }
    }
}
