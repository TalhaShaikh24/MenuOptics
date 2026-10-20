using MenuOptics.Models;
using MenuOptics.Models.DTOs.ActivityLog;

namespace MenuOptics.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(
            int? userId,
            string requestType,
            string? payload,
            string? ResponseDto,
            string logLevel,
            string? ipAddress = null
        );

        Task<DataTablesResponseDto<ActivityLogDto>> GetAllUserActivityLogsAsync(ActivityLogFilterDto? filter = null);
        Task<ActivityLogDto?> GetUserActivityLogByIdAsync(int logId);
        Task<IEnumerable<string>> GetRestaurantEndpointsAsync();
    }
}