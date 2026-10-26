using System;

namespace MenuOptics.Models.DTOs.PrivateVendor
{
    public class UpdatePrivateVendorDTO
    {
        public int PrivateVendorId { get; set; }
        public required string Name { get; set; }
    }
}
