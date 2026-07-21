namespace MenuOptics.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(
            int? vendorId,
            string requestId,
            string requestType,
            string? payload,
            string? response,
            string logLevel // "Success" | "Error" | "Warning"
        );
    }
}