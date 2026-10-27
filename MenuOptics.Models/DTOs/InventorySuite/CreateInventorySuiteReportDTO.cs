namespace MenuOptics.Models.DTOs
{
    public class CreateInventorySuiteReportDTO
    {
        public required DateTime StartDate { get; set; }
        public required DateTime EndDate { get; set; }
        public List<SaveInventorySuiteItemDTO>? Items { get; set; } = new();
    }
}
