using System;

namespace MenuOptics.Models.DTOs.Category
{
    public class GetCategoryDTO
    {
        public int CategoryId { get; set; }
        public int RestaurantId { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public int ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
