namespace MenuOptics.Models.DTOs
{
    public class GetEventDTO
    {
        public int EventId { get; set; }
        public int RestaurantId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public decimal PricePerGuest { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal FoodCostPercent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public string? Notes { get; set; }
        public string? ItemsJson { get; set; }
        public List<GetEventItemDTO> Items { get; set; } = new List<GetEventItemDTO>();
    }

}
