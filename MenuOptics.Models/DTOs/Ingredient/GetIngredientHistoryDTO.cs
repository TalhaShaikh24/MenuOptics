namespace MenuOptics.Models.DTOs
{
    public class GetIngredientHistoryDTO
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public int RestaurantId { get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
