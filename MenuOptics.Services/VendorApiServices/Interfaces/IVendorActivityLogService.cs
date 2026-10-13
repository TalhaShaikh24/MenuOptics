using MenuOptics.Models;
using MenuOptics.Models.DTOs.ActivityLog;

namespace MenuOptics.Services.VendorApiServices.Interfaces
{
    public interface IVendorActivityLogService
    {
        Task LogAsync(
            int? vendorId,
            string requestType,
            string? payload,
            string? ResponseDto,
            string logLevel // "Success" | "Error" | "Warning"
        );

        Task<DataTablesResponseDto<VendorActivityLogDto>> GetAllVendorActivityLogsAsync(ActivityLogFilterDto? filter = null);
        Task<VendorActivityLogDto?> GetVendorActivityLogByIdAsync(int logId);
    }
}