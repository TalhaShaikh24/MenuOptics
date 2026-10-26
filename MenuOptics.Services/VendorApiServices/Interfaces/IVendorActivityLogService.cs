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

        Task<DataTablesResponseDTO<GetVendorActivityLogDTO>> GetAllVendorActivityLogsAsync(FilterActivityLogDTO? filter = null);
        Task<GetVendorActivityLogDTO?> GetVendorActivityLogByIdAsync(int logId);
        Task<IEnumerable<string>> GetVendorEndpointsAsync();
    }
}