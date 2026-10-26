namespace MenuOptics.Models.DTOs
{
    public class GetPrivateVendorDTO
    {
        public int PrivateVendorId { get; set; }
        public int RestaurantId { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedOn { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public long ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
