using System;

namespace MenuOptics.Models
{
    public class ActivityLog
    {
        public int LogId { get; set; }
        public int? VendorId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public string? Response { get; set; }
        public string LogLevel { get; set; } = "Info"; // Success, Error, Warning
        public DateTime TimeStamp { get; set; }
    }
}
