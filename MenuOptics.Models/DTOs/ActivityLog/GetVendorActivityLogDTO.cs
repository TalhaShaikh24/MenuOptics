using System;

namespace MenuOptics.Models.DTOs.ActivityLog
{
    public class GetVendorActivityLogDTO
    {
        public int LogId { get; set; }
        public long? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorEmail { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public string? Response { get; set; }
        public string? LogLevel { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
