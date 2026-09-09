using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventorySuiteReportService
    {
        Task<ResponseDto> InventorySuiteReportAsync(InventorySuiteReportRequestDto model, int currentUserId);
    }
}
