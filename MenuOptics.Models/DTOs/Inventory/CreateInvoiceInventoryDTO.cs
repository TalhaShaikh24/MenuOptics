namespace MenuOptics.Models.DTOs
{
    public class CreateInvoiceInventoryDTO
    {
        public int PrivateVendorId { get; set; }
        public string? InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public List<SaveInvoiceItemDTO> Items { get; set; } = new();
        public bool UpdatePrices { get; set; } = false;
    }
}
