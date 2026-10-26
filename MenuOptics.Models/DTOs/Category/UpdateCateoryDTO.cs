using System;

namespace MenuOptics.Models.DTOs.Category
{
    public class UpdateCategoryDTO
    {
        public int CategoryId { get;set; }
        public required string Name { get; set; }
    }
}
