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
    }
}