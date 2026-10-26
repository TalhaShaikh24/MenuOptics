namespace MenuOptics.Models
{
    public class UpdateVendorIngredientDetailsDTO
    {
        public int IngredientId { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal? Size { get; set; }
        public int ClassificationId { get; set; }
        public string? ClassificationDescriptor { get; set; }
        public bool? IsActive { get; set; }
    }
}
