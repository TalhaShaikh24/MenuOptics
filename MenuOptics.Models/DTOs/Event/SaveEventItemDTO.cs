namespace MenuOptics.Models.DTOs
{
    public class SaveEventItemDTO
    {
        public int MenuItemId { get; set; }
        public int EventOnlyItemId { get; set; }
        public decimal Quantity { get;  set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
