namespace MenuOptics.Models.DTOs
{
    public class UpdateInvoiceInventoryDTO
    {
        public int InventoryInvoiceId { get; set; }
        public int PrivateVendorId { get; set; }
        public string? InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public List<SaveInvoiceItemDTO>? Items { get; set; }
        public bool UpdatePrices { get; set; }
    }
}
