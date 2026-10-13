using System;

namespace MenuOptics.Models.DTOs.ActivityLog
{
    public class ActivityLogDto
    {
        public int LogId { get; set; }
        public long? UserId { get; set; }
        public string? RestaurantName { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public string? Response { get; set; }
        public string? LogLevel { get; set; }
        public string? IpAddress { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
