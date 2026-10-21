using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPaymentService
    {
        (string SessionId, string? Url) CreateCheckoutSession(string? plan, int userId);
        Task<ResponseDto> PaymentSuccessAsync(string? sessionId, string? userId);
    }
}
