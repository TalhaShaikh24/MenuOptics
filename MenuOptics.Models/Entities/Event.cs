using System;
using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class Event
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
        public List<EventItem> Items { get; set; } = new List<EventItem>();
    }

    public class EventItem
    {
        public int EventItemId { get; set; }
        public int EventId { get; set; }
        public int MenuItemId { get; set; }
        public int EventOnlyItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Quantity { get => Qty; set => Qty = value; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal SellingPrice { get; set; }
    }
}
