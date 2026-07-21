using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IMenuEngineeringInsightsService
    {
        Task<Response> GetAllMenuEngineeringInsightsByUserIdAsync(int currentUserId, DateTime? startDate, DateTime? endDate);
    }
}
