using MenuOptics.Models;

namespace MenuOptics.Services
{
    public interface IInventorySuiteReportService
    {
        Task<ResponseDto> InventorySuiteReportAsync(InventorySuiteReportRequestDto model, int currentUserId);
        Task<ResponseDto> GetInventorySuiteReportsAsync(int currentUserId);
        Task<ResponseDto> UpdateInventorySuiteReportAsync(UpdateInventorySuiteReportRequestDto model, int currentUserId);
        Task<ResponseDto> AddInventorySuiteReportAsync(AddInventorySuiteReportRequestDto model, int currentUserId);
    }
}
