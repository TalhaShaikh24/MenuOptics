using Dapper;
using MenuOptics.Data;
using MenuOptics.Services.VendorApiServices.Interfaces;
using System.Data;

namespace MenuOptics.Services.VendorApiServices.Implementations
{
    public class VendorActivityLogService : IVendorActivityLogService
    {
        private readonly DapperContext _context;

        public VendorActivityLogService(DapperContext context)
        {
            _context = context;
        }

        public async Task LogAsync(
            int? vendorId,
            string requestType,
            string? payload,
            string? ResponseDto,
            string logLevel)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@VendorId", vendorId);
                parameters.Add("@RequestType", requestType);
                parameters.Add("@Payload", payload);
                parameters.Add("@Response", ResponseDto);
                parameters.Add("@LogLevel", logLevel);
                parameters.Add("@Action", "INSERT");

                await connection.ExecuteScalarAsync<int>("sp_VendorActivityLogCRUD", parameters, commandType: CommandType.StoredProcedure);

            }
            catch
            {

            }
        }

        public async Task<MenuOptics.Models.DataTablesResponseDto<MenuOptics.Models.DTOs.ActivityLog.VendorActivityLogDto>> GetAllVendorActivityLogsAsync(MenuOptics.Models.DTOs.ActivityLog.ActivityLogFilterDto? filter = null)
        {
            using var connection = _context.CreateConnection();
            var pageNumber = filter?.PageNumber > 0 ? filter.PageNumber : 1;
            var pageSize = filter?.PageSize ?? 10;

            var parameters = new DynamicParameters();
            parameters.Add("@Action", "SELECT_ALL");
            parameters.Add("@VendorId", filter?.VendorId);
            parameters.Add("@RequestType", filter?.RequestType);
            parameters.Add("@LogLevel", filter?.LogLevel);
            parameters.Add("@SearchValue", filter?.SearchValue);
            parameters.Add("@StartDate", filter?.StartDate);
            parameters.Add("@EndDate", filter?.EndDate);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var logs = await connection.QueryAsync<MenuOptics.Models.DTOs.ActivityLog.VendorActivityLogDto>(
                "sp_VendorActivityLogCRUD",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int totalCount = parameters.Get<int?>("@TotalCount") ?? 0;

            return new MenuOptics.Models.DataTablesResponseDto<MenuOptics.Models.DTOs.ActivityLog.VendorActivityLogDto>
            {
                Draw = filter?.Draw ?? 0,
                RecordsTotal = totalCount,
                RecordsFiltered = totalCount,
                Data = logs
            };
        }

        public async Task<MenuOptics.Models.DTOs.ActivityLog.VendorActivityLogDto?> GetVendorActivityLogByIdAsync(int logId)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Action", "SELECT");
            parameters.Add("@LogId", logId);

            var log = await connection.QueryFirstOrDefaultAsync<MenuOptics.Models.DTOs.ActivityLog.VendorActivityLogDto>(
                "sp_VendorActivityLogCRUD",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return log;
        }
    }
}