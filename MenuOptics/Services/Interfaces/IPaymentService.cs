using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPaymentService
    {
        Task<Response> PaymentSuccessAsync(string? sessionId, string? userId);
    }
}
