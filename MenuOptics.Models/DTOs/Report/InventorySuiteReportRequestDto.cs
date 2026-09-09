using System;

namespace MenuOptics.Models
{
    public class InventorySuiteReportRequestDto
    {
        public int RestaurantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
