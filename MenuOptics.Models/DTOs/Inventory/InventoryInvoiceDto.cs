using System;
using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class InventoryInvoiceDto
    {
        public int InventoryInvoiceId { get; set; }
        public long RestaurantId { get; set; }
        public long? VendorId { get; set; }
        public int? PrivateVendorId { get; set; }
        public string? VendorName { get; set; }
        public string? InvoiceId { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Total { get; set; }
        public string? ItemsJson { get; set; }
        public List<InventoryInvoiceItemDetailDto> Items { get; set; } = new List<InventoryInvoiceItemDetailDto>();
    }

    public class InventoryInvoiceItemDetailDto
    {
        public string? IngredientName { get; set; }
        public decimal Quantity { get; set; }
        public int? PackSize { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }
}
