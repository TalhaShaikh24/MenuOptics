namespace MenuOptics.Models.DTOs
{
    public class GetEventItemDTO
    {
        public int EventItemId { get; set; }
        public int EventId { get; set; }
        public int MenuItemId { get; set; }
        public int EventOnlyItemId { get; set; }
        public string? ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal SellingPrice { get; set; }
    }
}
