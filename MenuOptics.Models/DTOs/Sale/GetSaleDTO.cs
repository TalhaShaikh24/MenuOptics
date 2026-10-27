using System;
using System.Collections.Generic;

namespace MenuOptics.Models.DTOs
{
    public class GetSaleDTO
    {
        public int SaleId { get; set; }
        public int RestaurantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public List<GetSaleItemDTO>? Items { get; set; }
        public string? ItemsJson { get; set; } // For JSON serialization in query
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
