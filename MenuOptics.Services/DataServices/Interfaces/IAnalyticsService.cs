using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IAnalyticsService
    {
        Task<ResponseDto> GetAnalyticsDashboardAsync(int currentUserId, DateTime? startDate, DateTime? endDate);
    }
}
