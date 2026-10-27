namespace MenuOptics.Models.DTOs
{
    public class CreateEventOnlyItemDTO
    {
        public required string Name { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
    }
}
