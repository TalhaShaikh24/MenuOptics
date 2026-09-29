namespace MenuOptics.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(
            int? userId,
            string requestId,
            string requestType,
            string? payload,
            string? ResponseDto,
            string logLevel,
            string? ipAddress = null
        );
    }
}