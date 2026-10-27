using System;
using System.Collections.Generic;

namespace MenuOptics.Models.DTOs
{
    public class CreateSaleDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public List<GetSaleItemDTO>? Items { get; set; }
    }
}
