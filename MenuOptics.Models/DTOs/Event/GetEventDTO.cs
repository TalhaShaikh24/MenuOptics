namespace MenuOptics.Models.DTOs
{
    public class GetEventDTO
    {
        public int EventId { get; set; }
        public int RestaurantId { get; set; }
        public required string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public required string Location { get; set; } 
        public required string ContactName { get; set; } 
        public required string ContactEmail { get; set; } 
        public required string ContactPhone { get; set; } 
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
