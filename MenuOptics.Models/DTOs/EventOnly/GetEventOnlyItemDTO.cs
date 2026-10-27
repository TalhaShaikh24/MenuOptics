namespace MenuOptics.Models.DTOs
{
    public class GetEventOnlyItemDTO
    {
        public int EventOnlyItemId { get; set; }
        public string? Name { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
    }
}
