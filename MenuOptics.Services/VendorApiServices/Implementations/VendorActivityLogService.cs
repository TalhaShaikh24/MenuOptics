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
                parameters.Add("@VendorId", vendorId);
                parameters.Add("@RequestId", requestId);
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
    }
}