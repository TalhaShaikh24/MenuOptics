using MenuOptics.Models;
using MenuOptics.Models.DTOs;

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

        Task<DataTablesResponseDTO<GetActivityLogDTO>> GetAllUserActivityLogsAsync(FilterActivityLogDTO? filter = null);
        Task<GetActivityLogDTO?> GetUserActivityLogByIdAsync(int logId);
        Task<IEnumerable<string>> GetRestaurantEndpointsAsync();
    }
}