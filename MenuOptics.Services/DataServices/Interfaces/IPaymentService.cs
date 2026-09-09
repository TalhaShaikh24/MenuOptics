using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IPaymentService
    {
        Task<ResponseDto> PaymentSuccessAsync(string? sessionId, string? userId);
    }
}
