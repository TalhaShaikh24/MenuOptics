namespace MenuOptics.Models.DTOs
{
    public class CreateVendorInvoiceInventoryDTO
    {
        public int RestaurantId { get; set; }
        public string? InvoiceId { get; set; } // Optional
        public List<VendorInvoiceItemDTO>? Items { get; set; }
    }
}
