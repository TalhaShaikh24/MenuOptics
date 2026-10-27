namespace MenuOptics.Models.DTOs
{
    public class CreateEventDTO
    {
        public int RestaurantId { get; set; }
        public string? EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string? Location { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal PricePerGuest { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? Notes { get; set; }
        public List<SaveEventItemDTO> Items { get; set; } = new List<SaveEventItemDTO>();
    }
}
