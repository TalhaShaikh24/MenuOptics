namespace MenuOptics.Models
{
    public class RestaurantPriceEntryDto
    {
        public int RestaurantId { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
