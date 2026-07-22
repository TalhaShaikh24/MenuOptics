using System;

namespace MenuOptics.Models
{
    public class LinkVendorDto
    {
        public int? Id { get; set; }
        public int? VendorId { get; set; }
        public int? VendorIngredientId { get; set; }
        public int? RestaurantId { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
