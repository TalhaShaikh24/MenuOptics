using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class CreateVendorInvoiceInventoryDTO
    {
        public int RestaurantId { get; set; }
        public string? InvoiceId { get; set; } // Optional
        public List<VendorInvoiceItemDTO> Items { get; set; } = new();
    }
}
