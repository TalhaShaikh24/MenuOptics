using System;
using System.Collections.Generic;

namespace MenuOptics.Models
{
    public class Sale
    {
        public int SaleId { get; set; }
        public int? UserId { get; set; }
        public DateTime SaleDate { get; set; }
        public DateTime EndSaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public List<SaleItem>? Items { get; set; }
        public string? ItemsJson { get; set; } // For JSON serialization in query
       
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; }

        public string? Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class SaleItem
    {
        public int SaleId { get; set; }
        public int MenuItemId { get; set; }
        public string? MenuItemName { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
