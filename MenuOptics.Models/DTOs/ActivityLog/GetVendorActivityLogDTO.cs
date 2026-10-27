namespace MenuOptics.Models.DTOs
{
    public class GetVendorActivityLogDTO
    {
        public int LogId { get; set; }
        public long VendorId { get; set; }
        public required string VendorName { get; set; }
        public required string VendorEmail { get; set; }
        public required string RequestType { get; set; }
        public required string Payload { get; set; }
        public required string Response { get; set; }
        public required string LogLevel { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
