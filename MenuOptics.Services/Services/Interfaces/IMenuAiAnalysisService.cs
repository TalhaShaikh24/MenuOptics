using MenuOptics.Models;
using System;
using System.Threading.Tasks;

namespace MenuOptics.Services
{
    public interface IMenuAiAnalysisService
    {
        Task<ResponseDto> GenerateMenuAiAnalysisAsync(int currentUserId, DateTime? startDate, DateTime? endDate);
    }
}
