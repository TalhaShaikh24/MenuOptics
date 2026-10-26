using System;

namespace MenuOptics.Models
{
    public class SavePolicyDTO
    {
        public string PolicyType { get; set; } = string.Empty; // "Privacy", "Terms", "DataExport"
        public string Content { get; set; } = string.Empty;
    }
}
