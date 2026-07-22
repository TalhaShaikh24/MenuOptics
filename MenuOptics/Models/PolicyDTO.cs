namespace MenuOptics.Models
{
    public class PolicyDTO
    {
        public int PolicyId { get; set; }
        public string PolicyType { get; set; } // "Privacy", "Terms", "DataExport"
        public string Content { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
