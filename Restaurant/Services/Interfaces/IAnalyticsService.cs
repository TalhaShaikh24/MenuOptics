using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IAnalyticsService
    {
        Task<Response> GetAnalyticsDashboardAsync(int currentUserId, DateTime? startDate, DateTime? endDate);
    }
}
