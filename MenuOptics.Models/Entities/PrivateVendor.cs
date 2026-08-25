using System;

namespace MenuOptics.Models
{
    public class PrivateVendor
    {
        public int PrivateVendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public long CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public long? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
    public class MixedVendor
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsPrivate { get; set;}
        public bool IsActive { get; set;}
    }
}
