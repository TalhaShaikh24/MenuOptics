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
        public List<GetInvoiceItemDTO>? Items { get; set; }
    }
}
