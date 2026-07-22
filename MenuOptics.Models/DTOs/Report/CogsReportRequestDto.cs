using System;

namespace MenuOptics.Models
{
    public class CogsReportRequestDto
    {
        public int RestaurantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
