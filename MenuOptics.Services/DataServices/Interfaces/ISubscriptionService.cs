using MenuOptics.Models;
using MenuOptics.Models.DTOs.Subscription;

namespace MenuOptics.Services
{
    public interface ISubscriptionService
    {
        Task<ResponseDto> GetSubscriptionDetailsAsync(int effectiveUserId, int actualUserId, bool isParent);
        Task<ResponseDto> CancelSubscriptionAsync(int effectiveUserId, int actualUserId, bool isParent);
        Task<ResponseDto> CreateCheckoutSessionAsync(int effectiveUserId, string plan, string requestBaseUrl);
        Task<ResponseDto> RetryPaymentAsync(int effectiveUserId, int actualUserId, bool isParent, string requestBaseUrl);
    }
}
