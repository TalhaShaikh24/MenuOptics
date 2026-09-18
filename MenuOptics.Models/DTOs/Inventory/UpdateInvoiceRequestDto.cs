namespace MenuOptics.Models
{
    public class UpdateInvoiceRequestDto
    {
        public int InventoryInvoiceId { get; set; }
        public int PrivateVendorId { get; set; }
        public string? InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }
}
