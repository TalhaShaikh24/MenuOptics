namespace MenuOptics.Models
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public int RestaurantId { get; set; }
        public int? VendorId { get; set; }
        public int IngredientId { get; set; }
        public string? IngredientName { get; set; }
        public decimal Stock { get; set; }
        public decimal? Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class ClosingCountUpdateDto
    {
        public int RestaurantId { get; set; }
        public int IngredientId { get; set; }
        public decimal ClosingCount { get; set; }
        public DateTime ClosingDate { get; set; }
    }

    // DTO for a single item in the invoice
    public class InvoiceItem
    {
        public int IngredientId { get; set; }
        public decimal? Weight { get; set; }
        public decimal? PackSize { get; set; }
        public decimal? Quantity { get; set; }
        public decimal Price { get; set; }
    }

    // Request DTO for InvoiceInventory endpoint
    public class InvoiceInventoryRequest
    {
        public int RestaurantId { get; set; }
        public string? InvoiceId { get; set; } // Optional
        public List<InvoiceItem> Items { get; set; } = new();
    }
}