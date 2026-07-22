using Dapper;
using MenuOptics.Data;
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
            int? vendorId,
            string requestId,
            string requestType,
            string? payload,
            string? ResponseDto,
            string logLevel)
        {
            try
            {
                using var connection = _context.CreateConnection();

                var parameters = new DynamicParameters();
                parameters.Add("@VendorId", vendorId, DbType.Int32);
                parameters.Add("@RequestId", requestId, DbType.String);
                parameters.Add("@RequestType", requestType, DbType.String);
                parameters.Add("@Payload", payload, DbType.String);
                parameters.Add("@ResponseDto", ResponseDto, DbType.String);
                parameters.Add("@LogLevel", logLevel, DbType.String);
                parameters.Add("@TimeStamp", DateTime.UtcNow, DbType.DateTime);

                await connection.ExecuteAsync(
                    @"INSERT INTO ActivityLogs 
                        (VendorId, RequestId, RequestType, Payload, ResponseDto, LogLevel, TimeStamp)
                      VALUES 
                        (@VendorId, @RequestId, @RequestType, @Payload, @ResponseDto, @LogLevel, @TimeStamp)",
                    parameters);
            }
            catch
            {

            }
        }
    }
}