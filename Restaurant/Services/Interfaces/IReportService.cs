using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IReportService
    {
        Task<Response> CogsReportAsync(CogsReportRequest model, int currentUserId);
    }
}
