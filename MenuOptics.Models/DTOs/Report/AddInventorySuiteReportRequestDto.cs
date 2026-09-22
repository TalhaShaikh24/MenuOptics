using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class AddInventorySuiteReportRequestDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public List<InventorySuiteItemUpdateDto>? Items { get; set; } = new();
    }
}
