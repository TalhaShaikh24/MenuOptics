namespace MenuOptics.Models.DTOs
{
    public class UpdateInventorySuiteReportDTO
    {
        public int InventorySuiteId { get; set; }
        public List<SaveInventorySuiteItemDTO>? Items { get; set; }
    }
}
