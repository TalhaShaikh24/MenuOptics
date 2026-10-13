using System;

namespace MenuOptics.Models.DTOs.ActivityLog
{
    public class ActivityLogFilterDto
    {
        public int Draw { get; set; }
        public long? UserId { get; set; }
        public long? VendorId { get; set; }
        public string? RequestType { get; set; }
        public string? LogLevel { get; set; }
        public string? SearchValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
