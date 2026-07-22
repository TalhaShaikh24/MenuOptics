namespace MenuOptics.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public int RestaurantId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal PricePerGuest { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal FoodCostPercent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool? HasNote { get; set; }

        // 🔹 Items Collection
        public List<EventItem> Items { get; set; } = new List<EventItem>();
    }

    public class EventItem
    {
        public int EventItemId { get; set; }
        public int EventId { get; set; }
        public int MenuItemId { get; set; }
        public int EventOnlyItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public string QtyType { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string ItemSource { get; set; }
    }

    public class EventOnlyItemDTO
    {
        public int EventOnlyItemId { get; set; }
        public string Name { get; set; }
        public decimal DefaultCost { get; set; }
        public bool IsActive { get; set; }
    }

    public class EventNoteDTO
    {
        public int EventNoteId { get; set; }
        public int EventId { get; set; }
        public string? NoteText { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
