using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IStripeWebhookService
    {
        Task<(int StatusCode, object? Body)> HandleWebhookAsync(string json, string? stripeSignature);
        Task<ResponseDto> GetAllUsersAsync();
    }
}
