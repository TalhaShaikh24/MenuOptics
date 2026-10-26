namespace MenuOptics.Models
{
    public class RestaurantPriceEntryDTO
    {
        public int RestaurantId { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
