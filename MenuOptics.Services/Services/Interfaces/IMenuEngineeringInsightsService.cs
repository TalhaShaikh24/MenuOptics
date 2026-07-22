using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IMenuEngineeringInsightsService
    {
        Task<ResponseDto> GetAllMenuEngineeringInsightsByUserIdAsync(int currentUserId, DateTime? startDate, DateTime? endDate);
    }
}
