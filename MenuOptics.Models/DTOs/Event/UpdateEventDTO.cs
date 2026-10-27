namespace MenuOptics.Models.DTOs
{
    public class UpdateEventDTO
    {
        public int EventId { get; set; }
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
        public string? Notes { get; set; }
        public List<SaveEventItemDTO> Items { get; set; } = new List<SaveEventItemDTO>();
    }
}
