using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using MenuOptics.Models.DTOs.InventorySuite;

namespace MenuOptics.Services
{
    public interface IInventorySuiteReportService
    {
        Task<ResponseDto> InventorySuiteReportAsync(GetInventorySuiteReportFilterDTO model, int currentUserId);
        Task<ResponseDto> GetInventorySuiteReportsAsync(int currentUserId);
        Task<ResponseDto> UpdateInventorySuiteReportAsync(UpdateInventorySuiteReportDTO model, int currentUserId);
        Task<ResponseDto> AddInventorySuiteReportAsync(CreateInventorySuiteReportDTO model, int currentUserId);
    }
}
