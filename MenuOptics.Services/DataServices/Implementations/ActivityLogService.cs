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
    }
}