using Dapper;
using MenuOptics.Data;
using MenuOptics.Services.VendorApiServices.Interfaces;
using System.Data;

namespace MenuOptics.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly DapperContext _context;

        public ActivityLogService(DapperContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            int? userId,
            string requestType,
            string? payload,
            string? ResponseDto,
            string logLevel,
            string? ipAddress = null)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId);
                parameters.Add("@RequestType", requestType);
                parameters.Add("@Payload", payload);
                parameters.Add("@Response", ResponseDto);
                parameters.Add("@LogLevel", logLevel);
                parameters.Add("@IpAddress", ipAddress);
                parameters.Add("@Action", "INSERT");

                await connection.ExecuteScalarAsync<int>("sp_ActivityLogCRUD", parameters, commandType: CommandType.StoredProcedure);

            }
            catch
            {

            }
        }

        public async Task<MenuOptics.Models.DataTablesResponseDto<MenuOptics.Models.DTOs.ActivityLog.ActivityLogDto>> GetAllUserActivityLogsAsync(MenuOptics.Models.DTOs.ActivityLog.ActivityLogFilterDto? filter = null)
        {
            using var connection = _context.CreateConnection();
            var pageNumber = filter?.PageNumber > 0 ? filter.PageNumber : 1;
            var pageSize = filter?.PageSize ?? 10;

            var parameters = new DynamicParameters();
            parameters.Add("@Action", "SELECT_ALL");
            parameters.Add("@UserId", filter?.UserId);
            parameters.Add("@RequestType", filter?.RequestType);
            parameters.Add("@LogLevel", filter?.LogLevel);
            parameters.Add("@SearchValue", filter?.SearchValue);
            parameters.Add("@StartDate", filter?.StartDate);
            parameters.Add("@EndDate", filter?.EndDate);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var logs = await connection.QueryAsync<MenuOptics.Models.DTOs.ActivityLog.ActivityLogDto>(
                "sp_ActivityLogCRUD",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int totalCount = parameters.Get<int?>("@TotalCount") ?? 0;

            return new MenuOptics.Models.DataTablesResponseDto<MenuOptics.Models.DTOs.ActivityLog.ActivityLogDto>
            {
                Draw = filter?.Draw ?? 0,
                RecordsTotal = totalCount,
                RecordsFiltered = totalCount,
                Data = logs
            };
        }

        public async Task<MenuOptics.Models.DTOs.ActivityLog.ActivityLogDto?> GetUserActivityLogByIdAsync(int logId)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Action", "SELECT");
            parameters.Add("@LogId", logId);

            var log = await connection.QueryFirstOrDefaultAsync<MenuOptics.Models.DTOs.ActivityLog.ActivityLogDto>(
                "sp_ActivityLogCRUD",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return log;
        }
    }
}