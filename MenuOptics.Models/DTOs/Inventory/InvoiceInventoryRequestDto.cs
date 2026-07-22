using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class InvoiceInventoryRequestDto
    {
        public int RestaurantId { get; set; }
        public string? InvoiceId { get; set; } // Optional
        public List<InvoiceItemDto> Items { get; set; } = new();
    }
}
