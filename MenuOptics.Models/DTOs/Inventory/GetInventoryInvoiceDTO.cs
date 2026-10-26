namespace MenuOptics.Models.DTOs
{
    public class GetInventoryInvoiceDTO
    {
        public int InventoryInvoiceId { get; set; }
        public long RestaurantId { get; set; }
        public long? VendorId { get; set; }
        public int? PrivateVendorId { get; set; }
        public string? VendorName { get; set; }
        public string? InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Total { get; set; }
        public string? ItemsJson { get; set; }
        public List<InventoryInvoiceItemDetailDTO> Items { get; set; } = new List<InventoryInvoiceItemDetailDTO>();
    }

    public class InventoryInvoiceItemDetailDTO
    {
        public int? IngredientId { get; set; }
        public string? IngredientName { get; set; }
        public string? VendorProductId { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public int? PackSize { get; set; }
        public decimal? Weight { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }
}
