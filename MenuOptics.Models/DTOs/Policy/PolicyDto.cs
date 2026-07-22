using System;

namespace MenuOptics.Models
{
    public class PolicyDto
    {
        public int PolicyId { get; set; }
        public string PolicyType { get; set; } = string.Empty; // "Privacy", "Terms", "DataExport"
        public string Content { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}
