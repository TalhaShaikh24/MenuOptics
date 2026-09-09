using System.Net;
using System.Net.Mail;

namespace MenuOptics.Services
{
    public class SmtpEmailService : ISmtpEmailService
    {
        private readonly IConfiguration _config;
        public SmtpEmailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtp = _config["Email:SmtpHost"]!;
            var port = int.Parse(_config["Email:SmtpPort"]!);
            var user = _config["Email:Username"]!;
            var pass = _config["Email:Password"]!;
            var from = _config["Email:From"]!;

            using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(user, pass), EnableSsl = true };
            using var message = new MailMessage(from, toEmail, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(message);
        }
    }
}
