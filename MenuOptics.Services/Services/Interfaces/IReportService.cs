using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IReportService
    {
        Task<ResponseDto> CogsReportAsync(CogsReportRequestDto model, int currentUserId);
    }
}
