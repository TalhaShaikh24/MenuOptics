namespace MenuOptics.Services
{
    public interface ISmtpEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}
